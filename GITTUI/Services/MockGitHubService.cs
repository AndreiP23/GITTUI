using GITTUI.Models;
using Octokit;

namespace GITTUI.Services
{
    /// <summary>
    /// Deterministic in-memory IGitHubService used for controlled experiments.
    /// Simulates network latency with a configurable artificial delay so that
    /// processor-model comparisons are not confounded by API jitter or rate
    /// limiting. Activated via Program.cs when --mock is passed on the CLI.
    /// </summary>
    internal sealed class MockGitHubService : IGitHubService
    {
        private readonly MetricsService _metrics;
        private readonly TimeSpan _artificialDelay;
        private readonly List<GITRepositoryModel> _repos;

        public MockGitHubService(MetricsService metrics, TimeSpan? artificialDelay = null, int repoCount = 25)
        {
            _metrics = metrics;
            _artificialDelay = artificialDelay ?? TimeSpan.FromMilliseconds(50);
            _repos = Enumerable.Range(1, repoCount)
                .Select(i => new GITRepositoryModel
                {
                    Name = $"mock-repo-{i:D2}",
                    Owner = "mock-org",
                    Url = $"https://example.invalid/mock-org/mock-repo-{i:D2}",
                    Description = $"Synthetic repository #{i} for benchmark runs"
                })
                .ToList();
        }

        public async Task<List<GITRepositoryModel>> GetRepositoriesAsync()
        {
            await SimulateNetworkAsync();
            return _repos.ToList();
        }

        public async Task<IReadOnlyList<Workflow>> GetWorkflowRunsAsync(string owner, string repoName)
        {
            await SimulateNetworkAsync();
            return Array.Empty<Workflow>();
        }

        public Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName)
            => GetRepositoryActivityAsync(owner, repoName, 7);

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName, int days)
        {
            await SimulateNetworkAsync();
            return GenerateActivity(owner, repoName, days);
        }

        public async Task<IReadOnlyList<WorkflowJob>> GetWorkflowRunJobsAsync(string owner, string repoName, long runId)
        {
            await SimulateNetworkAsync();
            return Array.Empty<WorkflowJob>();
        }

        public async Task RerunFailedJobsAsync(string owner, string repoName, long runId)
        {
            await SimulateNetworkAsync();
        }

        public async Task CreateWorkflowFileAsync(string owner, string repoName, string fileName, string yamlContent, string commitMessage)
        {
            await SimulateNetworkAsync();
        }

        private async Task SimulateNetworkAsync()
        {
            if (_artificialDelay > TimeSpan.Zero)
                await Task.Delay(_artificialDelay);
            _metrics.RecordGitHubApiCall();
        }

        private static List<GITActivityModel> GenerateActivity(string owner, string repoName, int days)
        {
            var seed = HashCode.Combine(owner, repoName, days);
            var rng = new Random(seed);
            var count = 20 + rng.Next(0, 15);
            var now = DateTime.UtcNow;
            var statuses = new[] { WorkflowStatus.Completed, WorkflowStatus.InProgress, WorkflowStatus.Queued };
            var conclusions = new[] { WorkflowConclusion.Success, WorkflowConclusion.Failure, WorkflowConclusion.Cancelled };
            var events = new[] { WorkflowEvent.Push, WorkflowEvent.PullRequest };

            var activities = new List<GITActivityModel>(count);
            for (int i = 0; i < count; i++)
            {
                activities.Add(new GITActivityModel
                {
                    WorkflowName = $"workflow-{(i % 4) + 1}",
                    Status = statuses[rng.Next(statuses.Length)],
                    Conclusion = conclusions[rng.Next(conclusions.Length)],
                    CreatedAt = now.AddHours(-i * (days * 24.0 / count)),
                    Event = events[rng.Next(events.Length)],
                    RunId = 1000 + i,
                    LogsUrl = $"https://example.invalid/mock/{repoName}/runs/{1000 + i}"
                });
            }
            return activities;
        }
    }
}
