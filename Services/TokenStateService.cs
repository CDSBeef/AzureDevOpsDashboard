namespace AzureDevOpsDashboard.Services
{
    public class TokenStateService
    {
        public event Action? OnTokenSet;
        
        public void NotifyTokenSet()
        {
            OnTokenSet?.Invoke();
        }
    }
}
