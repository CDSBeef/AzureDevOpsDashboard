namespace AzureDevOpsDashboard.Data;

public class BuildInfo
{
    public int Id { get; set; }
    public string Definition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string BuildNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RequestedForDisplayName { get; set; } = string.Empty;
    public string TriggerInfo { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string FormattedUrl { get; set; } = string.Empty;

    // Add new properties
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public int DefinitionId { get; set; }
    public Repository? Repository { get; set; }

    public string DisplayName => string.IsNullOrEmpty(Message) 
        ? BuildNumber 
        : $"{BuildNumber} - {Message}";

    public TimeSpan? Duration => StartTime.HasValue && FinishTime.HasValue 
        ? FinishTime.Value - StartTime.Value 
        : null;

    public string DurationDisplay => Duration.HasValue 
        ? Duration.Value.TotalMinutes >= 1 
            ? $"{Math.Floor(Duration.Value.TotalMinutes)} min {Duration.Value.Seconds} sec"
            : $"{Duration.Value.Seconds} sec"
        : "In progress";

    public string FormattedBranch => SourceBranch.Replace("refs/heads/", "");
}
