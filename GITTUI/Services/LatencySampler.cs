using System.Collections.Concurrent;

namespace GITTUI.Services
{
    /// <summary>
    /// Thread-safe bounded ring buffer of latency samples (milliseconds) that
    /// produces percentile statistics on demand. Cap defaults to 500 samples;
    /// once full, the oldest sample is evicted on each new record.
    /// </summary>
    internal sealed class LatencySampler
    {
        private readonly ConcurrentQueue<long> _samples = new();
        private readonly int _capacity;

        public LatencySampler(int capacity = 500)
        {
            _capacity = capacity;
        }

        public void Record(TimeSpan elapsed)
        {
            _samples.Enqueue((long)elapsed.TotalMilliseconds);
            while (_samples.Count > _capacity && _samples.TryDequeue(out _))
            {
                // Trim oldest; loop handles concurrent writers overshooting capacity.
            }
        }

        public void Reset()
        {
            while (_samples.TryDequeue(out _)) { }
        }

        public LatencyStats GetStats()
        {
            var snapshot = _samples.ToArray();
            if (snapshot.Length == 0)
                return LatencyStats.Empty;

            Array.Sort(snapshot);
            var count = snapshot.Length;

            double sum = 0;
            for (int i = 0; i < count; i++) sum += snapshot[i];
            var mean = sum / count;

            double sumSquaredDeviation = 0;
            for (int i = 0; i < count; i++)
            {
                var d = snapshot[i] - mean;
                sumSquaredDeviation += d * d;
            }
            var stdDev = count > 1 ? Math.Sqrt(sumSquaredDeviation / (count - 1)) : 0;

            return new LatencyStats(
                Count: count,
                MeanMs: mean,
                StdDevMs: stdDev,
                P50Ms: Percentile(snapshot, 0.50),
                P95Ms: Percentile(snapshot, 0.95),
                P99Ms: Percentile(snapshot, 0.99));
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
    }

    internal sealed record LatencyStats(
        long Count,
        double MeanMs,
        double StdDevMs,
        double P50Ms,
        double P95Ms,
        double P99Ms)
    {
        public static readonly LatencyStats Empty = new(0, 0, 0, 0, 0, 0);
    }
}
