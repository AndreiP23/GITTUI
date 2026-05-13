using System.Diagnostics;

namespace GITTUI.Services
{
    internal sealed class TaskBenchmark
    {
        private readonly TaskProcessorFactory _factory;
        private readonly ICacheInvalidator? _cacheInvalidator;
        private readonly int _warmupRuns;
        private readonly int _measuredRuns;
        private readonly TimeSpan _interPassDelay;

        public TaskBenchmark(
            TaskProcessorFactory factory,
            ICacheInvalidator? cacheInvalidator = null,
            int warmupRuns = 5,
            int measuredRuns = 30,
            TimeSpan? interPassDelay = null)
        {
            _factory = factory;
            _cacheInvalidator = cacheInvalidator;
            _warmupRuns = warmupRuns;
            _measuredRuns = measuredRuns;
            _interPassDelay = interPassDelay ?? TimeSpan.FromMilliseconds(500);
        }

        public async Task<BenchmarkResult> RunAsync(Func<Task> workload)
        {
            var sequential = await MeasureAsync(TaskType.Sequential, workload);
            var lightweight = await MeasureAsync(TaskType.Lightweight, workload);
            var concurrent = await MeasureAsync(TaskType.Concurrent, workload);
            var isolated = await MeasureAsync(TaskType.Isolated, workload);

            return new BenchmarkResult(sequential, lightweight, concurrent, isolated, _warmupRuns, _measuredRuns);
        }

        private async Task<BenchmarkTimings> MeasureAsync(TaskType type, Func<Task> workload)
        {
            var processor = _factory.GetProcessor(type);

            for (int i = 0; i < _warmupRuns; i++)
            {
                _cacheInvalidator?.InvalidateAll();
                await processor.ProcessAsync(workload);
                if (_interPassDelay > TimeSpan.Zero)
                    await Task.Delay(_interPassDelay);
            }

            var timings = new long[_measuredRuns];
            for (int i = 0; i < _measuredRuns; i++)
            {
                _cacheInvalidator?.InvalidateAll();
                var sw = Stopwatch.StartNew();
                await processor.ProcessAsync(workload);
                sw.Stop();
                timings[i] = sw.ElapsedMilliseconds;
                if (_interPassDelay > TimeSpan.Zero && i < _measuredRuns - 1)
                    await Task.Delay(_interPassDelay);
            }

            return new BenchmarkTimings(timings);
        }
    }

    internal sealed record BenchmarkTimings
    {
        public long Min { get; }
        public long Max { get; }
        public double Mean { get; }
        public double StdDev { get; }
        public double P50 { get; }
        public double P95 { get; }
        public double P99 { get; }

        public BenchmarkTimings(long[] samples)
        {
            var copy = (long[])samples.Clone();
            Array.Sort(copy);
            Min = copy[0];
            Max = copy[^1];
            Mean = copy.Average();

            double sumSquaredDeviation = 0;
            foreach (var sample in copy)
            {
                var d = sample - Mean;
                sumSquaredDeviation += d * d;
            }
            StdDev = copy.Length > 1 ? Math.Sqrt(sumSquaredDeviation / (copy.Length - 1)) : 0;

            P50 = Percentile(copy, 0.50);
            P95 = Percentile(copy, 0.95);
            P99 = Percentile(copy, 0.99);
        }

        private static double Percentile(long[] sorted, double rank)
        {
            if (sorted.Length == 1) return sorted[0];
            var position = rank * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);
            if (lower == upper) return sorted[lower];
            var weight = position - lower;
            return sorted[lower] * (1 - weight) + sorted[upper] * weight;
        }

        public override string ToString() =>
            $"mean {Mean,5:F0}ms sd {StdDev,4:F0} p50 {P50,4:F0} p95 {P95,4:F0} p99 {P99,4:F0} min {Min} max {Max}";
    }

    internal sealed record BenchmarkResult(
        BenchmarkTimings Sequential,
        BenchmarkTimings Lightweight,
        BenchmarkTimings Concurrent,
        BenchmarkTimings Isolated,
        int WarmupRuns,
        int MeasuredRuns);
}
