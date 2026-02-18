using GITTUI.Models;
using Octokit;

namespace GITTUI.Services
{
    internal class GitHubService
    {
        private readonly GitHubClient _client;

        public GitHubService(string token)
        {
            _client = new GitHubClient(new ProductHeaderValue("Monitor"));
            _client.Credentials = new Credentials(token);
        }

        public async Task<List<GITRepositoryModel>> GetRepositoriesAsync()
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IReadOnlyList<Workflow>> GetWorkflowRunsAsync(string owner, string repoName)
        {
            return (await _client.Actions.Workflows.List(owner, repoName)).Workflows;
        }

        public async Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName)
        {
            // 1. Create the request object (used for filtering)
            // We leave it empty to get all types of runs
            var workflowRequest = new WorkflowRunsRequest();

            // 2. Create the pagination options
            var options = new ApiOptions
            {
                PageSize = 30,
                PageCount = 1
            };

            // 3. Call the List method using the 4-parameter overload:
            // List(string owner, string name, WorkflowRunsRequest request, ApiOptions options)
            var response = await _client.Actions.Workflows.Runs.List(owner, repoName, workflowRequest, options);

            return response.WorkflowRuns.Select(run => new GITActivityModel
            {
                WorkflowName = run.Name,
                Status = run.Status.StringValue,
                Conclusion = run.Conclusion?.StringValue,
                CreatedAt = run.CreatedAt.DateTime,
                Event = run.Event
            }).ToList();
        }
    }
}
