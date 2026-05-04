using System.Diagnostics;

namespace GITTUI.Services
{
    internal class TaskBenchmark
    {
        private readonly TaskProcessorFactory _factory;
        private const int WarmupRuns = 1;
        private const int MeasuredRuns = 5;

        public TaskBenchmark(TaskProcessorFactory factory)
        {
            _factory = factory;
        }

        public async Task<BenchmarkResult> RunAsync(Func<Task> workload)
        {
            var lightweight = await MeasureAsync(TaskType.Lightweight, workload);
            var concurrent = await MeasureAsync(TaskType.Concurrent, workload);
            var isolated = await MeasureAsync(TaskType.Isolated, workload);

            return new BenchmarkResult(lightweight, concurrent, isolated);
        }

        private async Task<BenchmarkTimings> MeasureAsync(TaskType type, Func<Task> workload)
        {
            var processor = _factory.GetProcessor(type);

            // Warmup — let JIT compile without affecting measurements
            for (int i = 0; i < WarmupRuns; i++)
                await processor.ProcessAsync(workload);

            var timings = new long[MeasuredRuns];
            for (int i = 0; i < MeasuredRuns; i++)
            {
                var sw = Stopwatch.StartNew();
                await processor.ProcessAsync(workload);
                sw.Stop();
                timings[i] = sw.ElapsedMilliseconds;
            }

            return new BenchmarkTimings(timings);
        }
    }

    internal record BenchmarkTimings
    {
        public long Min { get; }
        public long Max { get; }
        public double Mean { get; }
        public long Median { get; }

        public BenchmarkTimings(long[] samples)
        {
            Array.Sort(samples);
            Min = samples[0];
            Max = samples[^1];
            Mean = samples.Average();
            Median = samples[samples.Length / 2];
        }

        public override string ToString() =>
            $"Mean: {Mean:F0}ms | Median: {Median}ms | Min: {Min}ms | Max: {Max}ms";
    }

    internal record BenchmarkResult(BenchmarkTimings Lightweight, BenchmarkTimings Concurrent, BenchmarkTimings Isolated)
    {
        public override string ToString() =>
            $"Lightweight: {Lightweight}\nConcurrent:  {Concurrent}\nIsolated:    {Isolated}";
    }
}
