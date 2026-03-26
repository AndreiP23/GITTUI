using GITTUI.Models;
using Octokit;

namespace GITTUI.Services
{
    internal interface IGitHubService
    {
        Task<List<GITRepositoryModel>> GetRepositoriesAsync();
        Task<IReadOnlyList<Workflow>> GetWorkflowRunsAsync(string owner, string repoName);
        Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName);
        Task<List<GITActivityModel>> GetRepositoryActivityAsync(string owner, string repoName, int days);
        Task<IReadOnlyList<WorkflowJob>> GetWorkflowRunJobsAsync(string owner, string repoName, long runId);
        Task RerunFailedJobsAsync(string owner, string repoName, long runId);
        Task CreateWorkflowFileAsync(string owner, string repoName, string fileName, string yamlContent, string commitMessage);
    }
}
