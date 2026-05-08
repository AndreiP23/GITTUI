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
        private readonly CacheOptions _options;

        // Track every key we write so InvalidateAll can remove them precisely
        private readonly HashSet<string> _trackedKeys = new();
        private readonly object _keysLock = new();

        public CachingGitHubService(
            IGitHubService inner,
            IMemoryCache cache,
            ILogger<CachingGitHubService> logger,
            IOptions<CacheOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
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
                _logger.LogDebug("Cache hit: repositories");
                return hit;
            }

            _logger.LogInformation("Fetching repositories from GitHub API");
            var result = await _inner.GetRepositoriesAsync();
            return SetTracked(key, result, TimeSpan.FromSeconds(_options.RepositoriesTtlSeconds));
        }

        public async Task<IReadOnlyList<Workflow>> GetWorkflowRunsAsync(string owner, string repoName)
        {
            var key = $"workflows:{owner}/{repoName}";
            if (_cache.TryGetValue(key, out IReadOnlyList<Workflow>? hit) && hit is not null)
            {
                _logger.LogDebug("Cache hit: workflows {Owner}/{Repo}", owner, repoName);
                return hit;
            }

            _logger.LogInformation("Fetching workflows for {Owner}/{Repo}", owner, repoName);
            var result = await _inner.GetWorkflowRunsAsync(owner, repoName);
            return SetTracked(key, result, TimeSpan.FromSeconds(_options.ActivityTtlSeconds));
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName)
        {
            var key = $"activity:{owner}/{repoName}";
            if (_cache.TryGetValue(key, out List<GITActivityModel>? hit) && hit is not null)
            {
                _logger.LogDebug("Cache hit: activity {Owner}/{Repo}", owner, repoName);
                return hit;
            }

            _logger.LogInformation("Fetching activity for {Owner}/{Repo}", owner, repoName);
            var result = await _inner.GetRepositoryActivityAsync(owner, repoName);
            return SetTracked(key, result, TimeSpan.FromSeconds(_options.ActivityTtlSeconds));
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName, int days)
        {
            var key = $"activity:{owner}/{repoName}/{days}d";
            if (_cache.TryGetValue(key, out List<GITActivityModel>? hit) && hit is not null)
            {
                _logger.LogDebug("Cache hit: {Days}-day history {Owner}/{Repo}", days, owner, repoName);
                return hit;
            }

            _logger.LogInformation("Fetching {Days}-day history for {Owner}/{Repo}", days, owner, repoName);
            var result = await _inner.GetRepositoryActivityAsync(owner, repoName, days);
            return SetTracked(key, result, TimeSpan.FromSeconds(_options.ActivityTtlSeconds));
        }

        // These two are pass-through: job details are fetched on explicit user action,
        // and mutations must never be served stale.
        public Task<IReadOnlyList<WorkflowJob>> GetWorkflowRunJobsAsync(string owner, string repoName, long runId)
            => _inner.GetWorkflowRunJobsAsync(owner, repoName, runId);

        public Task RerunFailedJobsAsync(string owner, string repoName, long runId)
        {
            // Invalidate activity cache for this repo so the next load reflects the rerun
            RemoveTracked($"activity:{owner}/{repoName}");
            return _inner.RerunFailedJobsAsync(owner, repoName, runId);
        }

        public Task CreateWorkflowFileAsync(string owner, string repoName, string fileName, string yamlContent, string commitMessage)
            => _inner.CreateWorkflowFileAsync(owner, repoName, fileName, yamlContent, commitMessage);

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

        private void RemoveTracked(string key)
        {
            _cache.Remove(key);
            lock (_keysLock) _trackedKeys.Remove(key);
        }
    }
}
