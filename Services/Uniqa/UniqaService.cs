using Newtonsoft.Json;
using RestSharp;

namespace Oblak.Services.Uniqa
{
    public class UniqaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UniqaService> _logger;
        private RestClient _client;

        public UniqaService(IConfiguration configuration, ILogger<UniqaService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var client = new RestClient(_configuration["Uniqa:AuthEndpoint"]);
            var request = new RestRequest("oauth/token", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", _configuration["Uniqa:ClientId"]);
            request.AddParameter("client_secret", _configuration["Uniqa:ClientSecret"]);
            request.AddParameter("grant_type", "client_credentials");

            var response = await client.ExecuteAsync(request);

            _logger.LogInformation("====== UNIQA AUTH RESPONSE START ======");
            _logger.LogInformation("StatusCode: {StatusCode} ({StatusCodeInt})", response.StatusCode, (int)response.StatusCode);
            _logger.LogInformation("IsSuccessful: {IsSuccessful}", response.IsSuccessful);
            _logger.LogInformation("Error Message: {ErrorMessage}", response.ErrorMessage);
            _logger.LogInformation("Raw Content:\n{Content}", response.Content);
            _logger.LogInformation("====== UNIQA AUTH RESPONSE END ======");

            if (response.IsSuccessful && response.Content != null)
            {
                try
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content);
                    return content.access_token;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse access token response.");
                }
            }

            throw new Exception("Uniqa authentication failed");
        }

        public async Task<bool> SyncLeadAsync(object requestBody)
        {
            var token = await GetAccessTokenAsync();
            _client = new RestClient(_configuration["Uniqa:ApiEndpoint"]);

            var request = new RestRequest("v1/api/leads/intent", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("x-za-tenant", "uniqamne");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(requestBody);

            var response = await _client.ExecuteAsync(request);

            _logger.LogInformation("====== UNIQA SYNC LEAD RESPONSE START ======");
            _logger.LogInformation("StatusCode: {StatusCode} ({StatusCodeInt})", response.StatusCode, (int)response.StatusCode);
            _logger.LogInformation("IsSuccessful: {IsSuccessful}", response.IsSuccessful);
            _logger.LogInformation("Error Message: {ErrorMessage}", response.ErrorMessage);
            _logger.LogInformation("Raw Content:\n{Content}", response.Content);
            _logger.LogInformation("====== UNIQA SYNC LEAD RESPONSE END ======");

            return response.IsSuccessful;
        }
    }
}
