namespace AzureDevOpsDashboard.Data;

public class ReleaseStage
{
    public required string Name { get; set; }
    public required string Status { get; set; }
    public DateTime? LastReleaseDate { get; set; }
    public required string ReleaseName { get; set; }
    public required string StageName { get; set; }
}
