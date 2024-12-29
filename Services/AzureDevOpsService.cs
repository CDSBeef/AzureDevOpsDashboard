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
                        var repoName = repo.GetProperty("name").GetString();
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
                                try
                                {
                                    var rawJson = pr.GetRawText();
                                    _logger.LogInformation("Processing PR JSON: {Json}", rawJson);
                                    
                                    var options = new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    };
                                    
                                    var pullRequest = JsonSerializer.Deserialize<PullRequest>(rawJson, options);
                                    if (pullRequest != null)
                                    {
                                        // Format the URL
                                        pullRequest.FormattedUrl = $"https://dev.azure.com/{organization}/{project}/_git/{repoName}/pullrequest/{pullRequest.Id}";
                                        // Format the Repo URL
                                        pullRequest.FormattedRepoUrl = $"https://dev.azure.com/{organization}/{project}/_git/{repoName}";
                                        prs.Add(pullRequest);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to deserialize PR: {Json}", rawJson);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing PR JSON: {Json}", pr.GetRawText());
                                }
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

                _logger.LogInformation("Fetching Releases for org: {Organization}, project: {Project}", organization, project);

                // Get release definition
                var definitionsUrl = $"https://vsrm.dev.azure.com/{organization}/{project}/_apis/release/definitions?api-version=6.0&$expand=environments";
                _logger.LogInformation("Requesting definitions URL: {Url}", definitionsUrl);
                
                var definitionsResponse = await _httpClient.GetAsync(definitionsUrl);
                var definitionsContent = await definitionsResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Definitions Response: {Response}", definitionsContent);
                
                definitionsResponse.EnsureSuccessStatusCode();
                
                var stages = new List<ReleaseStage>();
                var definitions = JsonSerializer.Deserialize<JsonElement>(definitionsContent);

                if (definitions.TryGetProperty("value", out JsonElement definitionValues))
                {
                    foreach (var definition in definitionValues.EnumerateArray())
                    {
                        var releaseName = definition.GetProperty("name").GetString();
                        var definitionId = definition.GetProperty("id").GetInt32();

                        // Get the latest release for this definition
                        var releasesUrl = $"https://vsrm.dev.azure.com/{organization}/{project}/_apis/release/releases?definitionId={definitionId}&$top=1&$expand=environments&api-version=6.0";
                        _logger.LogInformation("Fetching latest release from URL: {Url}", releasesUrl);
                        
                        var releasesResponse = await _httpClient.GetAsync(releasesUrl);
                        var releasesContent = await releasesResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation("Latest release Response: {Response}", releasesContent);

                        if (releasesResponse.IsSuccessStatusCode)
                        {
                            var releaseData = JsonSerializer.Deserialize<JsonElement>(releasesContent);
                            if (releaseData.TryGetProperty("value", out JsonElement releases) && releases.GetArrayLength() > 0)
                            {
                                var latestRelease = releases[0];
                                if (latestRelease.TryGetProperty("environments", out JsonElement environments))
                                {
                                    foreach (var env in environments.EnumerateArray())
                                    {
                                        try
                                        {
                                            stages.Add(new ReleaseStage
                                            {
                                                ReleaseName = releaseName,
                                                StageName = env.GetProperty("name").GetString(),
                                                Name = env.GetProperty("name").GetString(),
                                                Status = env.GetProperty("status").GetString(),
                                                LastReleaseDate = env.TryGetProperty("lastModifiedOn", out JsonElement date) 
                                                    ? date.GetDateTime() 
                                                    : null
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Error processing environment: {EnvJson}", JsonSerializer.Serialize(env));
                                        }
                                    }
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

        public async Task<List<Release>> GetReleasesAsync()
        {
            try
            {
                await SetupHttpClientAsync();
                var organization = _configuration["AzureDevOps:Organization"];
                var project = _configuration["AzureDevOps:Project"];

                _logger.LogInformation("Fetching Releases for org: {Organization}, project: {Project}", organization, project);

                // Get all releases
                var releasesUrl = $"https://vsrm.dev.azure.com/{organization}/{project}/_apis/release/releases?$expand=environments&api-version=6.0";
                _logger.LogInformation("Requesting releases URL: {Url}", releasesUrl);
                
                var releasesResponse = await _httpClient.GetAsync(releasesUrl);
                var releasesContent = await releasesResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Releases Response: {Response}", releasesContent);
                
                releasesResponse.EnsureSuccessStatusCode();
                
                var releases = new List<Release>();
                var releasesData = JsonSerializer.Deserialize<JsonElement>(releasesContent);

                if (releasesData.TryGetProperty("value", out JsonElement releaseValues))
                {
                    foreach (var releaseValue in releaseValues.EnumerateArray())
                    {
                        var release = new Release
                        {
                            Name = releaseValue.GetProperty("name").GetString(),
                            DefinitionName = releaseValue.GetProperty("releaseDefinition").GetProperty("name").GetString(),
                            CreatedOn = releaseValue.GetProperty("createdOn").GetDateTime()
                        };

                        if (releaseValue.TryGetProperty("environments", out JsonElement environments))
                        {
                            foreach (var env in environments.EnumerateArray())
                            {
                                release.Stages.Add(new ReleaseStage
                                {
                                    Name = env.GetProperty("name").GetString(),
                                    Status = env.GetProperty("status").GetString(),
                                    LastReleaseDate = env.TryGetProperty("lastModifiedOn", out JsonElement date) 
                                        ? date.GetDateTime() 
                                        : null
                                });
                            }
                        }

                        releases.Add(release);
                    }
                }

                _logger.LogInformation("Retrieved {Count} releases", releases.Count);
                return releases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get releases");
                throw;
            }
        }
    }
}