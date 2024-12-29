namespace AzureDevOpsDashboard.Data;

public class PullRequest
{
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
}
