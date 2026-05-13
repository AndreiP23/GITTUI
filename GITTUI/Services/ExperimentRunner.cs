using System.Diagnostics;
using System.Globalization;
using System.Text;
using GITTUI.Models;
using Microsoft.Extensions.Logging;

namespace GITTUI.Services
{
    /// <summary>
    /// Runs the controlled experiment matrix (processor x debounce x cache state)
    /// described in the thesis plan. For each cell it executes N passes of a
    /// representative workload (repository activity load) and records per-pass
    /// UI latency, activity load and debounce wait stats. Output is a single
    /// CSV file with one row per matrix cell.
    /// </summary>
    internal sealed class ExperimentRunner
    {
        private static readonly int[] _debounceLevels = { 0, 100, 300 };
        private static readonly CacheState[] _cacheStates = { CacheState.Cold, CacheState.Warm };
        private static readonly TaskType[] _processors =
        {
            TaskType.Sequential,
            TaskType.Lightweight,
            TaskType.Concurrent,
            TaskType.Isolated
        };

        private readonly IGitHubService _gitHubService;
        private readonly ICacheInvalidator _cacheInvalidator;
        private readonly TaskProcessorFactory _processorFactory;
        private readonly RuntimeSettingsService _runtimeSettings;
        private readonly ILogger<ExperimentRunner> _logger;
        private readonly string _exportDirectory;

        public ExperimentRunner(
            IGitHubService gitHubService,
            ICacheInvalidator cacheInvalidator,
            TaskProcessorFactory processorFactory,
            RuntimeSettingsService runtimeSettings,
            ILogger<ExperimentRunner> logger)
        {
            _gitHubService = gitHubService;
            _cacheInvalidator = cacheInvalidator;
            _processorFactory = processorFactory;
            _runtimeSettings = runtimeSettings;
            _logger = logger;

            _exportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GITTUI", "logs", "experiments");
            Directory.CreateDirectory(_exportDirectory);
        }

        public async Task<ExperimentReport> RunAsync(
            GITRepositoryModel repo,
            int passesPerCell = 20,
            int warmupPasses = 3,
            IProgress<ExperimentProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var startedAt = DateTime.UtcNow;
            var originalProcessor = _processorFactory.CurrentTaskType;
            var originalSettings = _runtimeSettings.GetSnapshot();
            var cells = new List<ExperimentCellResult>();

            try
            {
                var totalCells = _processors.Length * _debounceLevels.Length * _cacheStates.Length;
                var cellIndex = 0;

                foreach (var processor in _processors)
                {
                    foreach (var debounceMs in _debounceLevels)
                    {
                        foreach (var cacheState in _cacheStates)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            cellIndex++;

                            progress?.Report(new ExperimentProgress(cellIndex, totalCells, processor, debounceMs, cacheState));

                            var cell = await RunCellAsync(
                                repo, processor, debounceMs, cacheState,
                                originalSettings, passesPerCell, warmupPasses, cancellationToken);
                            cells.Add(cell);

                            _logger.LogInformation(
                                "Experiment cell complete: {Processor} debounce={Debounce}ms cache={Cache} ui_p50={P50:F0}ms",
                                processor, debounceMs, cacheState, cell.UiLatency.P50Ms);
                        }
                    }
                }
            }
            finally
            {
                _processorFactory.SetCurrentTaskType(originalProcessor);
                _runtimeSettings.Update(originalSettings);
            }

            var csvPath = WriteCsv(cells, startedAt, repo);
            return new ExperimentReport(startedAt, DateTime.UtcNow, repo.Name, cells, csvPath);
        }

        private async Task<ExperimentCellResult> RunCellAsync(
            GITRepositoryModel repo,
            TaskType processorType,
            int debounceMs,
            CacheState cacheState,
            RuntimeSettings baseSettings,
            int measuredPasses,
            int warmupPasses,
            CancellationToken cancellationToken)
        {
            _processorFactory.SetCurrentTaskType(processorType);
            _runtimeSettings.Update(baseSettings with { DebounceDelayMs = debounceMs });
            var processor = _processorFactory.GetProcessor(processorType);

            // Warmup
            for (int i = 0; i < warmupPasses; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (cacheState == CacheState.Cold) _cacheInvalidator.InvalidateAll();
                await ExecutePassAsync(processor, repo, debounceMs, cancellationToken);
            }

            // Make sure warm-cache cells start with a populated cache
            if (cacheState == CacheState.Warm)
            {
                _cacheInvalidator.InvalidateAll();
                await _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name);
            }

            var uiSampler = new LatencySampler(measuredPasses);
            var activitySampler = new LatencySampler(measuredPasses);
            var debounceSampler = new LatencySampler(measuredPasses);

            for (int i = 0; i < measuredPasses; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (cacheState == CacheState.Cold) _cacheInvalidator.InvalidateAll();

                var uiWatch = Stopwatch.StartNew();

                if (debounceMs > 0)
                    await Task.Delay(debounceMs, cancellationToken);

                debounceSampler.Record(uiWatch.Elapsed);

                var loadWatch = Stopwatch.StartNew();
                await ExecutePassAsync(processor, repo, debounceMs: 0, cancellationToken);
                loadWatch.Stop();
                activitySampler.Record(loadWatch.Elapsed);

                uiWatch.Stop();
                uiSampler.Record(uiWatch.Elapsed);
            }

            return new ExperimentCellResult(
                processorType,
                debounceMs,
                cacheState,
                measuredPasses,
                uiSampler.GetStats(),
                activitySampler.GetStats(),
                debounceSampler.GetStats());
        }

        private async Task ExecutePassAsync(ITaskProcessor processor, GITRepositoryModel repo, int debounceMs, CancellationToken ct)
        {
            await processor.ProcessAsync(async _ =>
            {
                await _gitHubService.GetRepositoryActivityAsync(repo.Owner, repo.Name);
            }, ct);
        }

        private string WriteCsv(List<ExperimentCellResult> cells, DateTime startedAt, GITRepositoryModel repo)
        {
            var fileName = $"experiment-{startedAt:yyyyMMdd-HHmmss-fff}.csv";
            var path = Path.Combine(_exportDirectory, fileName);

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',', new[]
            {
                "started_at_utc",
                "repo",
                "processor",
                "debounce_ms",
                "cache_state",
                "pass_count",
                "ui_latency_count","ui_latency_mean_ms","ui_latency_stddev_ms","ui_latency_p50_ms","ui_latency_p95_ms","ui_latency_p99_ms",
                "activity_load_count","activity_load_mean_ms","activity_load_stddev_ms","activity_load_p50_ms","activity_load_p95_ms","activity_load_p99_ms",
                "debounce_wait_count","debounce_wait_mean_ms","debounce_wait_stddev_ms","debounce_wait_p50_ms","debounce_wait_p95_ms","debounce_wait_p99_ms"
            }));

            foreach (var c in cells)
            {
                var fields = new List<string>
                {
                    startedAt.ToString("O"),
                    Escape($"{repo.Owner}/{repo.Name}"),
                    c.Processor.ToString(),
                    c.DebounceMs.ToString(CultureInfo.InvariantCulture),
                    c.CacheState.ToString(),
                    c.PassCount.ToString(CultureInfo.InvariantCulture)
                };
                AppendStats(fields, c.UiLatency);
                AppendStats(fields, c.ActivityLoad);
                AppendStats(fields, c.DebounceWait);

                sb.AppendLine(string.Join(',', fields));
            }

            File.WriteAllText(path, sb.ToString());
            return path;
        }

        private static void AppendStats(List<string> fields, LatencyStats s)
        {
            fields.Add(s.Count.ToString(CultureInfo.InvariantCulture));
            fields.Add(s.MeanMs.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(s.StdDevMs.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(s.P50Ms.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(s.P95Ms.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(s.P99Ms.ToString("F2", CultureInfo.InvariantCulture));
        }

        private static string Escape(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }

    internal enum CacheState
    {
        Cold,
        Warm
    }

    internal sealed record ExperimentCellResult(
        TaskType Processor,
        int DebounceMs,
        CacheState CacheState,
        int PassCount,
        LatencyStats UiLatency,
        LatencyStats ActivityLoad,
        LatencyStats DebounceWait);

    internal sealed record ExperimentReport(
        DateTime StartedAtUtc,
        DateTime CompletedAtUtc,
        string Repo,
        IReadOnlyList<ExperimentCellResult> Cells,
        string CsvPath);

    internal sealed record ExperimentProgress(
        int CellIndex,
        int TotalCells,
        TaskType Processor,
        int DebounceMs,
        CacheState CacheState);
}
