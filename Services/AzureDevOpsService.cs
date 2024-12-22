using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AzureDevOpsDashboard.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureDevOpsDashboard.Services
{
    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureDevOpsService> _logger;
        
        public AzureDevOpsService(HttpClient httpClient, ITokenService tokenService, 
            IConfiguration configuration, ILogger<AzureDevOpsService> logger)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task SetupHttpClientAsync()
        {
            try
            {
                var token = await _tokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("PAT token is empty");
                }

                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes($"azure:{token}")));
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AzureDevOpsDashboard/1.0");

                _logger.LogInformation("HTTP Client headers set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup HTTP client");
                throw;
            }
        }

        public async Task<List<PullRequest>> GetPullRequestsAsync()
        {
            try
            {
                await SetupHttpClientAsync();
                var organization = _configuration["AzureDevOps:Organization"];
                var project = _configuration["AzureDevOps:Project"];

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    _logger.LogError("Organization or Project configuration is missing");
                    throw new Exception("Organization or Project configuration is missing");
                }

                _logger.LogInformation("Fetching PRs for org: {Organization}, project: {Project}", organization, project);

                // First, get all repositories in the project
                var reposUrl = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories?api-version=7.0";
                _logger.LogInformation("Requesting URL: {Url}", reposUrl);
                _logger.LogInformation("Fetching repositories from URL: {Url}", reposUrl);
                var reposResponse = await _httpClient.GetAsync(reposUrl);
                _logger.LogInformation("Repos response status: {StatusCode}", reposResponse.StatusCode);
                string reposContentPreview = (await reposResponse.Content.ReadAsStringAsync())[..Math.Min(200, (await reposResponse.Content.ReadAsStringAsync()).Length)];
                _logger.LogInformation("Repos content preview (first 200 chars): {Preview}", reposContentPreview);

                if (!reposResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch repositories. Status code: {StatusCode}, Reason: {ReasonPhrase}", reposResponse.StatusCode, reposResponse.ReasonPhrase);
                    return new List<PullRequest>();
                }

                var reposContent = await reposResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Repositories response: {Response}", reposContent);

                var prs = new List<PullRequest>();
                var reposResult = JsonSerializer.Deserialize<JsonElement>(reposContent);

                if (reposResult.TryGetProperty("value", out JsonElement repos))
                {
                    foreach (var repo in repos.EnumerateArray())
                    {
                        var repoId = repo.GetProperty("id").GetString();
                        var prUrl = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repoId}/pullrequests?searchCriteria.status=active&api-version=7.0";
                        _logger.LogInformation("Requesting URL: {Url}", prUrl);
                        _logger.LogInformation("Fetching PRs from URL: {Url}", prUrl);
                        var prResponse = await _httpClient.GetAsync(prUrl);
                        _logger.LogInformation("PR response status: {StatusCode}", prResponse.StatusCode);
                        string prContentPreview = (await prResponse.Content.ReadAsStringAsync())[..Math.Min(200, (await prResponse.Content.ReadAsStringAsync()).Length)];
                        _logger.LogInformation("PR content preview (first 200 chars): {Preview}", prContentPreview);

                        if (!prResponse.IsSuccessStatusCode)
                        {
                            _logger.LogError("Failed to fetch pull requests for repository {RepoId}. Status code: {StatusCode}, Reason: {ReasonPhrase}", repoId, prResponse.StatusCode, prResponse.ReasonPhrase);
                            continue;
                        }

                        var prContent = await prResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation("Pull requests response for repo {RepoId}: {Response}", repoId, prContent);

                        var prResult = JsonSerializer.Deserialize<JsonElement>(prContent);
                        if (prResult.TryGetProperty("value", out JsonElement prArray))
                        {
                            foreach (var pr in prArray.EnumerateArray())
                            {
                                var pullRequest = JsonSerializer.Deserialize<PullRequest>(pr.GetRawText());
                                prs.Add(pullRequest);
                            }
                        }
                    }
                }

                return prs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching pull requests");
                throw;
            }
        }

        public async Task<List<BuildInfo>> GetBuildsAsync()
        {
            try
            {
                await SetupHttpClientAsync();
                var organization = _configuration["AzureDevOps:Organization"];
                var project = _configuration["AzureDevOps:Project"];

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    _logger.LogError("Organization or Project configuration is missing");
                    throw new Exception("Organization or Project configuration is missing");
                }

                _logger.LogInformation("Fetching Builds for org: {Organization}, project: {Project}", organization, project);

                var url = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds?api-version=7.0";
                _logger.LogInformation("Requesting URL: {Url}", url);
                _logger.LogInformation("Fetching builds from URL: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("Build/Release response status: {StatusCode}", response.StatusCode);
                string contentPreview = (await response.Content.ReadAsStringAsync())[..Math.Min(200, (await response.Content.ReadAsStringAsync()).Length)];
                _logger.LogInformation("Build/Release content preview (first 200 chars): {Preview}", contentPreview);

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Build Response: {Response}", content);

                response.EnsureSuccessStatusCode();
                
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                var builds = new List<BuildInfo>();

                if (result.TryGetProperty("value", out JsonElement valueElement))
                {
                    foreach (var build in valueElement.EnumerateArray())
                    {
                        try
                        {
                            builds.Add(new BuildInfo
                            {
                                Id = build.GetProperty("id").GetInt32(),
                                Definition = build.GetProperty("definition").GetProperty("name").GetString(),
                                Status = build.GetProperty("status").GetString(),
                                StartTime = build.GetProperty("startTime").GetDateTime(),
                                FinishTime = build.GetProperty("finishTime").GetDateTime()
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing build JSON: {BuildJson}", JsonSerializer.Serialize(build));
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} builds", builds.Count);
                return builds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get builds");
                throw;
            }
        }

        public async Task<List<ReleaseStage>> GetReleaseStagesAsync()
        {
            try
            {
                await SetupHttpClientAsync();
                var organization = _configuration["AzureDevOps:Organization"];
                var project = _configuration["AzureDevOps:Project"];

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    _logger.LogError("Organization or Project configuration is missing");
                    throw new Exception("Organization or Project configuration is missing");
                }

                _logger.LogInformation("Fetching Releases for org: {Organization}, project: {Project}", organization, project);

                // Updated Release API URL format and version
                var url = $"https://vsrm.dev.azure.com/{organization}/{project}/_apis/release/definitions?api-version=6.0";
                _logger.LogInformation("Requesting URL: {Url}", url);
                _logger.LogInformation("Fetching releases from URL: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("Build/Release response status: {StatusCode}", response.StatusCode);
                string contentPreview = (await response.Content.ReadAsStringAsync())[..Math.Min(200, (await response.Content.ReadAsStringAsync()).Length)];
                _logger.LogInformation("Build/Release content preview (first 200 chars): {Preview}", contentPreview);

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Release Response: {Response}", content);

                response.EnsureSuccessStatusCode();
                
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                var stages = new List<ReleaseStage>();

                if (result.TryGetProperty("value", out JsonElement valueElement))
                {
                    foreach (var release in valueElement.EnumerateArray())
                    {
                        if (release.TryGetProperty("environments", out JsonElement environments))
                        {
                            foreach (var stage in environments.EnumerateArray())
                            {
                                try
                                {
                                    stages.Add(new ReleaseStage
                                    {
                                        Name = stage.GetProperty("name").GetString(),
                                        Status = stage.GetProperty("status").GetString(),
                                        DeploymentsCount = stage.GetProperty("deploymentCount").GetInt32()
                                    });
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing release stage JSON: {StageJson}", JsonSerializer.Serialize(stage));
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} release stages", stages.Count);
                return stages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get release stages");
                throw;
            }
        }
    }
}