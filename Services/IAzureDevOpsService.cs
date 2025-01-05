using System.Threading.Tasks;
using System.Collections.Generic;
using AzureDevOpsDashboard.Data;

namespace AzureDevOpsDashboard.Services
{
    public interface IAzureDevOpsService
    {
        Task<List<Project>> GetProjectsAsync();  // New method
        Task<List<PullRequest>> GetPullRequestsAsync(string project);  // Modified to accept project
        Task<List<BuildInfo>> GetBuildsAsync(string project);  // Modified to accept project
        Task<List<ReleaseStage>> GetReleaseStagesAsync(string project);  // Modified to accept project
        Task<List<Release>> GetReleasesAsync(string project);  // Modified to accept project
    }
}