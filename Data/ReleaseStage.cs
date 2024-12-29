namespace AzureDevOpsDashboard.Data;

public class ReleaseStage
{
        public string Name { get; set; }
        public string Status { get; set; }
        public DateTime? LastReleaseDate { get; set; }  // Changed from LastUpdatedOn
        public string ReleaseName { get; set; }
        public string StageName { get; set; }
}
