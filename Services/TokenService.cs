using Microsoft.Extensions.Logging;

namespace AzureDevOpsDashboard.Services
{
    public class TokenService : ITokenService
    {
        private string? _token;
        private readonly object _lock = new object();
        private readonly ILogger<TokenService> _logger;

        public TokenService(ILogger<TokenService> logger)
        {
            _logger = logger;
        }

        public Task<string> GetTokenAsync()
        {
            lock (_lock)
            {
                var tokenStatus = string.IsNullOrEmpty(_token) ? "empty" : $"set (length: {_token.Length})";
                _logger.LogInformation("GetTokenAsync called. Token is {Status}", tokenStatus);
                return Task.FromResult(_token ?? string.Empty);
            }
        }

        public Task SetTokenAsync(string token)
        {
            lock (_lock)
            {
                _logger.LogInformation("Setting new token. Input token length: {Length}", token?.Length ?? 0);
                if (string.IsNullOrEmpty(token))
                {
                    var ex = new ArgumentException("Token cannot be empty");
                    _logger.LogError(ex, "Token validation failed");
                    throw ex;
                }
                
                _token = token;
                _logger.LogInformation("Token stored successfully. Stored token length: {Length}", _token.Length);
                return Task.CompletedTask;
            }
        }
    }
}