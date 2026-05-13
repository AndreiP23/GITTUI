using GITTUI.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace GITTUI.Services
{
    internal class GitHubService : IGitHubService
    {
        private readonly GitHubClient _client;
        private readonly ILogger<GitHubService> _logger;
        private readonly GitHubOptions _options;
        private readonly MetricsService _metrics;

        public GitHubService(string token, ILogger<GitHubService> logger, IOptions<GitHubOptions> options, MetricsService metrics)
        {
            _client = new GitHubClient(new ProductHeaderValue("Monitor"));
            _client.Credentials = new Credentials(token);
            _logger = logger;
            _options = options.Value;
            _metrics = metrics;
        }

        public async Task<List<GITRepositoryModel>> GetRepositoriesAsync()
        {
            _logger.LogDebug("Calling GitHub API: GetAllForCurrent");
            var octoRepos = await MeasureCallAsync(() => _client.Repository.GetAllForCurrent());

            if (octoRepos == null) throw new Exception("GitHub API returned no data.");

            return octoRepos.Select(r => new GITRepositoryModel
            {
                Name = r.Name ?? throw new Exception("Repo name missing from API!"),
                Owner = r.Owner.Login,
                Description = r.Description,
                Url = r.HtmlUrl
            }).ToList();
        }

        public async Task<IReadOnlyList<Workflow>> GetWorkflowRunsAsync(string owner, string repoName)
        {
            return (await MeasureCallAsync(() => _client.Actions.Workflows.List(owner, repoName))).Workflows;
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName)
        {
            var workflowRequest = new WorkflowRunsRequest();
            var options = new ApiOptions
            {
                PageSize = _options.PageSize,
                PageCount = _options.PageCount
            };

            var response = await MeasureCallAsync(() => _client.Actions.Workflows.Runs.List(owner, repoName, workflowRequest, options));
            return MapWorkflowRuns(response.WorkflowRuns);
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName, int days)
        {
            var workflowRequest = new WorkflowRunsRequest()
            {
                Created = $">={DateTime.UtcNow.AddDays(-days):yyyy-MM-dd}"
            };

            var response = await MeasureCallAsync(() => _client.Actions.Workflows.Runs.List(owner, repoName, workflowRequest));
            return MapWorkflowRuns(response.WorkflowRuns);
        }

        private static List<GITActivityModel> MapWorkflowRuns(IReadOnlyList<WorkflowRun> runs)
        {
            return runs.Select(run => new GITActivityModel
            {
                WorkflowName = run.Name,
                Status = Enum.TryParse<WorkflowStatus>(run.Status.StringValue, true, out var status) ? status : WorkflowStatus.Unknown,
                Conclusion = Enum.TryParse<WorkflowConclusion>(run.Conclusion?.StringValue, true, out var conclusion) ? conclusion : WorkflowConclusion.Unknown,
                CreatedAt = run.CreatedAt.DateTime,
                Event = Enum.TryParse<WorkflowEvent>(run.Event, true, out var workflowEvent) ? workflowEvent : WorkflowEvent.Unknown,
                RunId = run.Id,
                LogsUrl = run.HtmlUrl
            }).ToList();
        }

        public async Task<IReadOnlyList<WorkflowJob>> GetWorkflowRunJobsAsync(string owner, string repoName, long runId)
        {
            var response = await MeasureCallAsync(() => _client.Actions.Workflows.Jobs.List(owner, repoName, runId));
            return response.Jobs;
        }

        public async Task RerunFailedJobsAsync(string owner, string repoName, long runId)
        {
            await MeasureCallAsync(() => _client.Actions.Workflows.Runs.RerunFailedJobs(owner, repoName, runId));
        }

        public async Task CreateWorkflowFileAsync(string owner, string repoName, string fileName, string yamlContent, string commitMessage)
        {
            var path = $".github/workflows/{fileName}";
            var request = new CreateFileRequest(commitMessage, yamlContent);
            await MeasureCallAsync(() => _client.Repository.Content.CreateFile(owner, repoName, path, request));
        }

        private async Task<T> MeasureCallAsync<T>(Func<Task<T>> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await action();
            }
            finally
            {
                stopwatch.Stop();
                _metrics.RecordGitHubApiCall();
            }
        }

        private async Task MeasureCallAsync(Func<Task> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await action();
            }
            finally
            {
                stopwatch.Stop();
                _metrics.RecordGitHubApiCall();
            }
        }
    }
}
