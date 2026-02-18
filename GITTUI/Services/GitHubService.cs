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

        public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync()
        {
            return await _client.Repository.GetAllForCurrent();
        }

        public async Task<IReadOnlyList<Workflow>> GetWorkflowRunsAsync(string owner, string repoName)
        {
            return (await _client.Actions.Workflows.List(owner, repoName)).Workflows;
        }
    }
}
