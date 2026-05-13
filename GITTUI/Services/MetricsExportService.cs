using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GITTUI.Services
{
    internal sealed class MetricsExportService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        private readonly string _exportDirectory;

        public MetricsExportService()
        {
            _exportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GITTUI", "logs", "metrics");
            Directory.CreateDirectory(_exportDirectory);
        }

        public string ExportJson(MetricsSnapshot snapshot, RuntimeSettings settings, TaskType currentTaskType)
        {
            var export = new MetricsExportRecord(
                TimestampUtc: DateTime.UtcNow,
                Processor: currentTaskType.ToString(),
                Settings: settings,
                Snapshot: snapshot);

            var filePath = BuildFilePath("json");
            var json = JsonSerializer.Serialize(export, _jsonOptions);

            File.WriteAllText(filePath, json);
            return filePath;
        }

        public string ExportCsv(MetricsSnapshot snapshot, RuntimeSettings settings, TaskType currentTaskType)
        {
            var filePath = BuildFilePath("csv");
            var csv = new StringBuilder();

            csv.AppendLine(string.Join(',', new[]
            {
                "timestampUtc",
                "processor",
                "historyDays",
                "debounceDelayMs",
                "repositoriesTtlSeconds",
                "activityTtlSeconds",
                "autoRefreshEnabled",
                "autoRefreshIntervalSeconds",
                "manualRefreshCount",
                "autoRefreshCount",
                "cacheHitCount",
                "cacheMissCount",
                "cacheHitRate",
                "gitHubApiCallCount",
                "cancelledSelectionCount",
                "staleSelectionCount",
                "repoLoad_count","repoLoad_mean","repoLoad_stddev","repoLoad_p50","repoLoad_p95","repoLoad_p99",
                "activityLoad_count","activityLoad_mean","activityLoad_stddev","activityLoad_p50","activityLoad_p95","activityLoad_p99",
                "runDetailsLoad_count","runDetailsLoad_mean","runDetailsLoad_stddev","runDetailsLoad_p50","runDetailsLoad_p95","runDetailsLoad_p99",
                "uiLatency_count","uiLatency_mean","uiLatency_stddev","uiLatency_p50","uiLatency_p95","uiLatency_p99",
                "debounceWait_count","debounceWait_mean","debounceWait_stddev","debounceWait_p50","debounceWait_p95","debounceWait_p99"
            }));

            var fields = new List<string>
            {
                DateTime.UtcNow.ToString("O"),
                Escape(currentTaskType.ToString()),
                settings.HistoryDays.ToString(CultureInfo.InvariantCulture),
                settings.DebounceDelayMs.ToString(CultureInfo.InvariantCulture),
                settings.RepositoriesTtlSeconds.ToString(CultureInfo.InvariantCulture),
                settings.ActivityTtlSeconds.ToString(CultureInfo.InvariantCulture),
                settings.AutoRefreshEnabled.ToString(CultureInfo.InvariantCulture),
                settings.AutoRefreshIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                snapshot.ManualRefreshCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AutoRefreshCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CacheHitCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CacheMissCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CacheHitRate.ToString("F4", CultureInfo.InvariantCulture),
                snapshot.GitHubApiCallCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CancelledSelectionCount.ToString(CultureInfo.InvariantCulture),
                snapshot.StaleSelectionCount.ToString(CultureInfo.InvariantCulture)
            };

            AppendLatencyFields(fields, snapshot.RepositoryLoad);
            AppendLatencyFields(fields, snapshot.ActivityLoad);
            AppendLatencyFields(fields, snapshot.RunDetailsLoad);
            AppendLatencyFields(fields, snapshot.UiLatency);
            AppendLatencyFields(fields, snapshot.DebounceWait);

            csv.AppendLine(string.Join(',', fields));
            File.WriteAllText(filePath, csv.ToString());
            return filePath;
        }

        private static void AppendLatencyFields(List<string> fields, LatencyStats stats)
        {
            fields.Add(stats.Count.ToString(CultureInfo.InvariantCulture));
            fields.Add(stats.MeanMs.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(stats.StdDevMs.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(stats.P50Ms.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(stats.P95Ms.ToString("F2", CultureInfo.InvariantCulture));
            fields.Add(stats.P99Ms.ToString("F2", CultureInfo.InvariantCulture));
        }

        private string BuildFilePath(string extension)
        {
            var fileName = $"metrics-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.{extension}";
            return Path.Combine(_exportDirectory, fileName);
        }

        private static string Escape(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }

    internal sealed record MetricsExportRecord(
        DateTime TimestampUtc,
        string Processor,
        RuntimeSettings Settings,
        MetricsSnapshot Snapshot);
}
