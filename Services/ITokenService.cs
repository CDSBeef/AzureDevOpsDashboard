namespace AzureDevOpsDashboard.Services
{
    public interface ITokenService
    {
        event EventHandler TokenCleared;
        Task<string> GetTokenAsync();
        Task SetTokenAsync(string token);
        Task ClearTokenAsync();
    }
}