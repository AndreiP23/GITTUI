using GITTUI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace GITTUI.Services
{
    internal sealed class CachingGitHubService : IGitHubService, ICacheInvalidator
    {
        private readonly IGitHubService _inner;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingGitHubService> _logger;
        private readonly MetricsService _metrics;
        private readonly RuntimeSettingsService _runtimeSettings;

        // Track every key we write so InvalidateAll can remove them precisely
        private readonly HashSet<string> _trackedKeys = new();
        private readonly object _keysLock = new();

        public CachingGitHubService(
            IGitHubService inner,
            IMemoryCache cache,
            ILogger<CachingGitHubService> logger,
            MetricsService metrics,
            RuntimeSettingsService runtimeSettings)
        {
            _inner = inner;
            _cache = cache;
            _logger = logger;
            _metrics = metrics;
            _runtimeSettings = runtimeSettings;
        }

        // ── Read-through helpers ─────────────────────────────────────────────

        private T SetTracked<T>(string key, T value, TimeSpan ttl)
        {
            _cache.Set(key, value, ttl);
            lock (_keysLock) _trackedKeys.Add(key);
            return value;
        }

        // ── IGitHubService ───────────────────────────────────────────────────

        public async Task<List<GITRepositoryModel>> GetRepositoriesAsync()
        {
            const string key = "repos";
            if (_cache.TryGetValue(key, out List<GITRepositoryModel>? hit) && hit is not null)
            {
                _metrics.RecordCacheLookup(isHit: true);
                _logger.LogDebug("Cache hit: repositories");
                return hit;
            }

            _metrics.RecordCacheLookup(isHit: false);

            _logger.LogInformation("Fetching repositories from GitHub API");
            var result = await _inner.GetRepositoriesAsync();
            return SetTracked(key, result, TimeSpan.FromSeconds(_runtimeSettings.GetSnapshot().RepositoriesTtlSeconds));
        }

        public async Task<IReadOnlyList<Workflow>> GetWorkflowRunsAsync(string owner, string repoName)
        {
            var key = $"workflows:{owner}/{repoName}";
            if (_cache.TryGetValue(key, out IReadOnlyList<Workflow>? hit) && hit is not null)
            {
                _metrics.RecordCacheLookup(isHit: true);
                _logger.LogDebug("Cache hit: workflows {Owner}/{Repo}", owner, repoName);
                return hit;
            }

            _metrics.RecordCacheLookup(isHit: false);

            _logger.LogInformation("Fetching workflows for {Owner}/{Repo}", owner, repoName);
            var result = await _inner.GetWorkflowRunsAsync(owner, repoName);
            return SetTracked(key, result, TimeSpan.FromSeconds(_runtimeSettings.GetSnapshot().ActivityTtlSeconds));
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName)
        {
            var key = $"activity:{owner}/{repoName}";
            if (_cache.TryGetValue(key, out List<GITActivityModel>? hit) && hit is not null)
            {
                _metrics.RecordCacheLookup(isHit: true);
                _logger.LogDebug("Cache hit: activity {Owner}/{Repo}", owner, repoName);
                return hit;
            }

            _metrics.RecordCacheLookup(isHit: false);

            _logger.LogInformation("Fetching activity for {Owner}/{Repo}", owner, repoName);
            var result = await _inner.GetRepositoryActivityAsync(owner, repoName);
            return SetTracked(key, result, TimeSpan.FromSeconds(_runtimeSettings.GetSnapshot().ActivityTtlSeconds));
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName, int days)
        {
            var key = $"activity:{owner}/{repoName}/{days}d";
            if (_cache.TryGetValue(key, out List<GITActivityModel>? hit) && hit is not null)
            {
                _metrics.RecordCacheLookup(isHit: true);
                _logger.LogDebug("Cache hit: {Days}-day history {Owner}/{Repo}", days, owner, repoName);
                return hit;
            }

            _metrics.RecordCacheLookup(isHit: false);

            _logger.LogInformation("Fetching {Days}-day history for {Owner}/{Repo}", days, owner, repoName);
            var result = await _inner.GetRepositoryActivityAsync(owner, repoName, days);
            return SetTracked(key, result, TimeSpan.FromSeconds(_runtimeSettings.GetSnapshot().ActivityTtlSeconds));
        }

        // Pass-through: run jobs are transient enough that caching adds no value.
        public Task<IReadOnlyList<WorkflowJob>> GetWorkflowRunJobsAsync(string owner, string repoName, long runId)
            => _inner.GetWorkflowRunJobsAsync(owner, repoName, runId);

        public async Task RerunFailedJobsAsync(string owner, string repoName, long runId)
        {
            await _inner.RerunFailedJobsAsync(owner, repoName, runId);

            // Invalidate current activity plus all cached history windows for this repo.
            RemoveTrackedByPrefix($"activity:{owner}/{repoName}");
        }

        public async Task CreateWorkflowFileAsync(string owner, string repoName, string fileName, string yamlContent, string commitMessage)
        {
            await _inner.CreateWorkflowFileAsync(owner, repoName, fileName, yamlContent, commitMessage);

            RemoveTrackedByPrefix($"workflows:{owner}/{repoName}");
            RemoveTrackedByPrefix($"activity:{owner}/{repoName}");
        }

        // ── ICacheInvalidator ────────────────────────────────────────────────

        public void InvalidateAll()
        {
            lock (_keysLock)
            {
                foreach (var key in _trackedKeys)
                    _cache.Remove(key);
                _trackedKeys.Clear();
            }
            _logger.LogInformation("Cache fully invalidated");
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void RemoveTrackedByPrefix(string prefix)
        {
            List<string> keysToRemove;

            lock (_keysLock)
            {
                keysToRemove = _trackedKeys
                    .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                    .ToList();

                foreach (var key in keysToRemove)
                    _trackedKeys.Remove(key);
            }

            foreach (var key in keysToRemove)
                _cache.Remove(key);
        }
    }
}
