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

    public class ReleaseStage
    {
        public string ReleaseName { get; set; }
        public string StageName { get; set; }
        public string Status { get; set; }
        public DateTime? LastReleaseDate { get; set; }
    }
}