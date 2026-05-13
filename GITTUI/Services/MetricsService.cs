using System.Threading;

namespace GITTUI.Services
{
    internal sealed class MetricsService
    {
        private long _manualRefreshCount;
        private long _autoRefreshCount;
        private long _cacheHitCount;
        private long _cacheMissCount;
        private long _gitHubApiCallCount;
        private long _cancelledSelectionCount;
        private long _staleSelectionCount;

        private readonly LatencySampler _repositoryLoad = new();
        private readonly LatencySampler _activityLoad = new();
        private readonly LatencySampler _runDetailsLoad = new();
        private readonly LatencySampler _uiLatency = new();
        private readonly LatencySampler _debounceWait = new();

        public void RecordManualRefresh() => Interlocked.Increment(ref _manualRefreshCount);

        public void RecordAutoRefresh() => Interlocked.Increment(ref _autoRefreshCount);

        public void RecordCacheLookup(bool isHit)
        {
            if (isHit)
                Interlocked.Increment(ref _cacheHitCount);
            else
                Interlocked.Increment(ref _cacheMissCount);
        }

        public void RecordGitHubApiCall() => Interlocked.Increment(ref _gitHubApiCallCount);

        public void RecordCancelledSelection() => Interlocked.Increment(ref _cancelledSelectionCount);

        public void RecordStaleSelection() => Interlocked.Increment(ref _staleSelectionCount);

        public void RecordRepositoryLoad(TimeSpan elapsed) => _repositoryLoad.Record(elapsed);

        public void RecordActivityLoad(TimeSpan elapsed) => _activityLoad.Record(elapsed);

        public void RecordRunDetailsLoad(TimeSpan elapsed) => _runDetailsLoad.Record(elapsed);

        public void RecordUiLatency(TimeSpan elapsed) => _uiLatency.Record(elapsed);

        public void RecordDebounceWait(TimeSpan elapsed) => _debounceWait.Record(elapsed);

        public MetricsSnapshot GetSnapshot()
        {
            var cacheHits = Interlocked.Read(ref _cacheHitCount);
            var cacheMisses = Interlocked.Read(ref _cacheMissCount);
            var cacheLookups = cacheHits + cacheMisses;

            return new MetricsSnapshot(
                ManualRefreshCount: Interlocked.Read(ref _manualRefreshCount),
                AutoRefreshCount: Interlocked.Read(ref _autoRefreshCount),
                CacheHitCount: cacheHits,
                CacheMissCount: cacheMisses,
                CacheHitRate: cacheLookups == 0 ? 0 : (double)cacheHits / cacheLookups,
                GitHubApiCallCount: Interlocked.Read(ref _gitHubApiCallCount),
                CancelledSelectionCount: Interlocked.Read(ref _cancelledSelectionCount),
                StaleSelectionCount: Interlocked.Read(ref _staleSelectionCount),
                RepositoryLoad: _repositoryLoad.GetStats(),
                ActivityLoad: _activityLoad.GetStats(),
                RunDetailsLoad: _runDetailsLoad.GetStats(),
                UiLatency: _uiLatency.GetStats(),
                DebounceWait: _debounceWait.GetStats());
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _manualRefreshCount, 0);
            Interlocked.Exchange(ref _autoRefreshCount, 0);
            Interlocked.Exchange(ref _cacheHitCount, 0);
            Interlocked.Exchange(ref _cacheMissCount, 0);
            Interlocked.Exchange(ref _gitHubApiCallCount, 0);
            Interlocked.Exchange(ref _cancelledSelectionCount, 0);
            Interlocked.Exchange(ref _staleSelectionCount, 0);
            _repositoryLoad.Reset();
            _activityLoad.Reset();
            _runDetailsLoad.Reset();
            _uiLatency.Reset();
            _debounceWait.Reset();
        }
    }

    internal sealed record MetricsSnapshot(
        long ManualRefreshCount,
        long AutoRefreshCount,
        long CacheHitCount,
        long CacheMissCount,
        double CacheHitRate,
        long GitHubApiCallCount,
        long CancelledSelectionCount,
        long StaleSelectionCount,
        LatencyStats RepositoryLoad,
        LatencyStats ActivityLoad,
        LatencyStats RunDetailsLoad,
        LatencyStats UiLatency,
        LatencyStats DebounceWait);
}
