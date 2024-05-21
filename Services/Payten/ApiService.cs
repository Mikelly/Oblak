using RestSharp;
using System.Text.Json;

namespace Oblak.Services.Payten
{
    public class ApiService
    {
        RestClient _client;
        Dictionary<string, string> _authTokens;
        ILogger<ApiService> _logger;
        IConfiguration _configuration;

        public ApiService(ILogger<ApiService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new RestClient(_configuration["PAYTEN:URL"]!);
            _authTokens = new Dictionary<string, string>();
        }


        public async Task<Tuple<AuthorizeResponse, Error>> Authorize(AuthorizeRequest request)
        {
            var restRequest = new RestRequest("/authorize", Method.Post).AddJsonBody(request);

            var response = await _client.ExecutePostAsync(restRequest);

            if (response.Content == null) 
            {
                Console.WriteLine($"Request: ApplicationLoginID = {request.ApplicationLoginID}, Password = {request.Password}");
                Console.WriteLine("Response Content is null.");
                Console.WriteLine($"Full Response: {response}");
            }

            try
            {
                if (response.IsSuccessStatusCode)
                {
                    var authReponse = JsonSerializer.Deserialize<AuthorizeResponse>(response.Content);
                    if (_authTokens.ContainsKey(request.ApplicationLoginID)) _authTokens[request.ApplicationLoginID] = authReponse.Token;
                    else _authTokens.Add(request.ApplicationLoginID, authReponse.Token);
                    return new Tuple<AuthorizeResponse, Error>(authReponse, null);
                }
                else
                {
                    var authError = JsonSerializer.Deserialize<Error>(response.Content);
                    return new Tuple<AuthorizeResponse, Error>(null, authError);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in Authorize method: {ex.Message}");
                Console.WriteLine($"Request: ApplicationLoginID = {request.ApplicationLoginID}, Password = {request.Password}");
                if (response.Content != null)
                {
                    Console.WriteLine($"Response Content: {response.Content}");
                }
                else
                {
                    Console.WriteLine($"Full Response: {response}");
                }
                return new Tuple<AuthorizeResponse, Error>(null, new Error { description = ex.Message });
            }
        }


        public async Task<Tuple<CreatePaymentSessionTokenResponse, Error>> CreatePaymentSessionToken(CreatePaymentSessionTokenRequest request, string token)
        {
            var restRequest = new RestRequest("/createpaymentsessiontoken", Method.Post).AddJsonBody(request);
            restRequest.AddHeader("Authorization", $"Bearer {token}");

            var response = await _client.ExecutePostAsync(restRequest);

            if (response.IsSuccessStatusCode)
            {
                var tokenReponse = JsonSerializer.Deserialize<CreatePaymentSessionTokenResponse>(response.Content);
                return new Tuple<CreatePaymentSessionTokenResponse, Error>(tokenReponse, null);
            }
            else
            {
                var authError = JsonSerializer.Deserialize<Error>(response.Content);
                return new Tuple<CreatePaymentSessionTokenResponse, Error>(null, authError);
            }
        }

        public async Task<Tuple<CreatePaymentSessionTokenResponse, Error>> CancelPaymentSessionToken(CancelPaymentSessionTokenRequest request, string token)
        {
            var restRequest = new RestRequest("/createpaymentsessiontoken", Method.Post).AddJsonBody(request);
            restRequest.AddHeader("Authorization", $"Bearer {token}");

            var response = await _client.ExecutePostAsync(restRequest);

            if (response.IsSuccessStatusCode)
            {
                var tokenReponse = JsonSerializer.Deserialize<CreatePaymentSessionTokenResponse>(response.Content);
                return new Tuple<CreatePaymentSessionTokenResponse, Error>(tokenReponse, null);
            }
            else
            {
                var authError = JsonSerializer.Deserialize<Error>(response.Content);
                return new Tuple<CreatePaymentSessionTokenResponse, Error>(null, authError);
            }
        }

        public async Task<GetPaymentSessionStatusResponse> GetPaymentSessionStatus(GetPaymentSessionStatusRequest request)
        {
            var restRequest = new RestRequest("/getpaymentsessionstatus", Method.Post).AddJsonBody(request);

            var response = await _client.ExecutePostAsync(restRequest);

            if (response.IsSuccessStatusCode)
            {
                var statusReposne = JsonSerializer.Deserialize<GetPaymentSessionStatusResponse>(response.Content);
                return statusReposne;
            }
            else
            {
                var authError = JsonSerializer.Deserialize<Error>(response.Content);
                return null;
            }
        }

        public async Task<GetTransactionByPaymentSessionTokenResponse> GetTransactionByPaymentSessionToken(GetTransactionByPaymentSessionTokenRequest request)
        {
            var restRequest = new RestRequest("/gettransactionbypaymentsessiontoken", Method.Post).AddJsonBody(request);

            var response = await _client.ExecutePostAsync(restRequest);

            if (response.IsSuccessStatusCode)
            {
                var statusReposne = JsonSerializer.Deserialize<GetTransactionByPaymentSessionTokenResponse>(response.Content);
                return statusReposne;
            }
            else
            {
                var authError = JsonSerializer.Deserialize<Error>(response.Content);
                return null;
            }
        }
    }
}
