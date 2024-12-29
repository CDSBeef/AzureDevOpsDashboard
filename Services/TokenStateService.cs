namespace AzureDevOpsDashboard.Services
{
    public class TokenStateService
    {
        public event Action? OnTokenSet;
        public event Action? OnTokenCleared;
        
        public void NotifyTokenSet()
        {
            OnTokenSet?.Invoke();
        }

        public void NotifyTokenCleared()
        {
            OnTokenCleared?.Invoke();
        }
    }
}
