namespace AzureDevOpsDashboard.Services
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
        Task SetTokenAsync(string token);
    }
}