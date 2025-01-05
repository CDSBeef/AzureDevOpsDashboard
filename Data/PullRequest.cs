using System.Text.Json.Serialization;

namespace AzureDevOpsDashboard.Data;

public class PullRequest
{
    [JsonPropertyName("pullRequestId")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("creationDate")]
    public DateTime CreationDate { get; set; }

    [JsonPropertyName("createdBy")]
    public CreatedBy? CreatedBy { get; set; }

    [JsonPropertyName("sourceRefName")]
    public string? SourceRefName { get; set; }

    [JsonPropertyName("targetRefName")]
    public string? TargetRefName { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonIgnore]
    public string? FormattedUrl { get; set; }

    [JsonIgnore]
    public string? FormattedRepoUrl { get; set; }

    [JsonPropertyName("repository")]
    public Repository? Repository { get; set; }
}

public class CreatedBy
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}
