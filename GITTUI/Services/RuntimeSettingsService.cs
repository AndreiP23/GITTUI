using Microsoft.Extensions.Options;

namespace GITTUI.Services
{
    internal sealed class RuntimeSettingsService
    {
        private readonly object _lock = new();
        private RuntimeSettings _current;

        public RuntimeSettingsService(
            IOptions<GitHubOptions> gitHubOptions,
            IOptions<CacheOptions> cacheOptions,
            IOptions<AutoRefreshOptions> autoRefreshOptions)
        {
            _current = new RuntimeSettings(
                HistoryDays: gitHubOptions.Value.HistoryDays,
                DebounceDelayMs: gitHubOptions.Value.DebounceDelayMs,
                RepositoriesTtlSeconds: cacheOptions.Value.RepositoriesTtlSeconds,
                ActivityTtlSeconds: cacheOptions.Value.ActivityTtlSeconds,
                AutoRefreshEnabled: autoRefreshOptions.Value.Enabled,
                AutoRefreshIntervalSeconds: autoRefreshOptions.Value.IntervalSeconds);
        }

        public RuntimeSettings GetSnapshot()
        {
            lock (_lock)
                return _current;
        }

        public void Update(RuntimeSettings settings)
        {
            lock (_lock)
                _current = settings;
        }
    }

    internal sealed record RuntimeSettings(
        int HistoryDays,
        int DebounceDelayMs,
        int RepositoriesTtlSeconds,
        int ActivityTtlSeconds,
        bool AutoRefreshEnabled,
        int AutoRefreshIntervalSeconds);
}