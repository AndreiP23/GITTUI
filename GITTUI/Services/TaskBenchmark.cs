using System.Diagnostics;

namespace GITTUI.Services
{
    internal class TaskBenchmark
    {
        private readonly TaskProcessorFactory _factory;

        public TaskBenchmark(TaskProcessorFactory factory)
        {
            _factory = factory;
        }

        public async Task<BenchmarkResult> RunAsync(Func<Task> workload)
        {
            var lightweightMs = await MeasureAsync(TaskType.Lightweight, workload);
            var concurrentMs = await MeasureAsync(TaskType.Concurrent, workload);
            var isolatedMs = await MeasureAsync(TaskType.Isolated, workload);

            return new BenchmarkResult(lightweightMs, concurrentMs, isolatedMs);
        }

        private async Task<long> MeasureAsync(TaskType type, Func<Task> workload)
        {
            var processor = _factory.GetProcessor(type);
            var sw = Stopwatch.StartNew();
            await processor.ProcessAsync(workload);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    internal record BenchmarkResult(long LightweightMs, long ConcurrentMs, long IsolatedMs)
    {
        public override string ToString() =>
            $"Lightweight: {LightweightMs}ms | Concurrent: {ConcurrentMs}ms | Isolated: {IsolatedMs}ms";
    }
}
