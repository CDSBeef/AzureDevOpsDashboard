namespace AzureDevOpsDashboard.Data;

public class Release
{
    public required string Name { get; set; }
    public required string DefinitionName { get; set; }
    public DateTime? CreatedOn { get; set; }
    public List<ReleaseStage> Stages { get; set; } = new();
}
