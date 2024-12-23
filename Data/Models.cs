namespace AzureDevOpsDashboard.Data
{
    public class PullRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public class BuildInfo
    {
        public int Id { get; set; }
        public string Definition { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
    }

    public class Release
    {
        public string Name { get; set; }
        public string DefinitionName { get; set; }
        public DateTime? CreatedOn { get; set; }
        public List<ReleaseStage> Stages { get; set; } = new();
    }

    public class ReleaseStage
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public DateTime? LastReleaseDate { get; set; }  // Changed from LastUpdatedOn
        public string ReleaseName { get; set; }
        public string StageName { get; set; }
    }
}