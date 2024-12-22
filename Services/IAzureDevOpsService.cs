using System.Threading.Tasks;
using System.Collections.Generic;
using AzureDevOpsDashboard.Data;

namespace AzureDevOpsDashboard.Services
{
    public interface IAzureDevOpsService
    {
        Task<List<PullRequest>> GetPullRequestsAsync();
        Task<List<BuildInfo>> GetBuildsAsync();
        Task<List<ReleaseStage>> GetReleaseStagesAsync();
    }
}