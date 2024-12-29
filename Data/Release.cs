namespace AzureDevOpsDashboard.Data;

public class Release
{
        public string Name { get; set; }
        public string DefinitionName { get; set; }
        public DateTime? CreatedOn { get; set; }
        public List<ReleaseStage> Stages { get; set; } = new();
}
