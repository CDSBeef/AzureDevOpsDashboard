namespace AzureDevOpsDashboard.Data;

public class Release
{
    public required string Name { get; set; }
    public required string DefinitionName { get; set; }
    public DateTime? CreatedOn { get; set; }
    public List<ReleaseStage> Stages { get; set; } = new();
    public int? DefinitionId { get; set; }
    public int ReleaseId { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string FormattedBranch => Branch.Replace("refs/heads/", "");
}
