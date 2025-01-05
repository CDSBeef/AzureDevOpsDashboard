namespace AzureDevOpsDashboard.Data
{
    public class Project
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Visibility { get; set; } = string.Empty;
    }
}
