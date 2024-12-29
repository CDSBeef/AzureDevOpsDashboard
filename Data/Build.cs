namespace AzureDevOpsDashboard.Data;

public class BuildInfo
{
    public int Id { get; set; }
    public string Definition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? FinishTime { get; set; }
}
