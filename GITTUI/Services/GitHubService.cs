using GITTUI.Models;
using Octokit;

namespace GITTUI.Services
{
    internal class GitHubService : IGitHubService
    {
        private readonly GitHubClient _client;

        public GitHubService(string token)
        {
            _client = new GitHubClient(new ProductHeaderValue("Monitor"));
            _client.Credentials = new Credentials(token);
        }

        public async Task<List<GITRepositoryModel>> GetRepositoriesAsync()
        {
            var octoRepos = await _client.Repository.GetAllForCurrent();

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
            return (await _client.Actions.Workflows.List(owner, repoName)).Workflows;
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName)
        {
            var workflowRequest = new WorkflowRunsRequest();
            var options = new ApiOptions
            {
                PageSize = 30,
                PageCount = 1
            };

            var response = await _client.Actions.Workflows.Runs.List(owner, repoName, workflowRequest, options);

            return response.WorkflowRuns.Select(run => new GITActivityModel
            {
                WorkflowName = run.Name,
                Status = Enum.TryParse<WorkflowStatus>(run.Status.StringValue, true, out var status) ? status : WorkflowStatus.Unknown,
                Conclusion = Enum.TryParse<WorkflowConclusion>(run.Conclusion?.StringValue, true, out var conclusion) ? conclusion : WorkflowConclusion.Unknown,
                CreatedAt = run.CreatedAt.DateTime,
                Event = Enum.TryParse<WorkflowEvent>(run.Event, true, out var workflowEvent) ? workflowEvent : WorkflowEvent.Unknown
            }).ToList();
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName, int days)
        {
            var workflowRequest = new WorkflowRunsRequest()
            {
                Created = $">={DateTime.UtcNow.AddDays(-days):yyyy-MM-dd}"
            };

            var response = await _client.Actions.Workflows.Runs.List(owner, repoName, workflowRequest);

            return response.WorkflowRuns.Select(run => new GITActivityModel
            {
                WorkflowName = run.Name,
                Status = Enum.TryParse<WorkflowStatus>(run.Status.StringValue, true, out var status) ? status : WorkflowStatus.Unknown,
                Conclusion = Enum.TryParse<WorkflowConclusion>(run.Conclusion?.StringValue, true, out var conclusion) ? conclusion : WorkflowConclusion.Unknown,
                CreatedAt = run.CreatedAt.DateTime,
                Event = Enum.TryParse<WorkflowEvent>(run.Event, true, out var workflowEvent) ? workflowEvent : WorkflowEvent.Unknown
            }).ToList();
        }
    }
}
