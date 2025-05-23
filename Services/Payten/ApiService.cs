using RestSharp;
using System.Security.Cryptography;
using System.Text;
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

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var authReponse = JsonSerializer.Deserialize<AuthorizeResponse>(response.Content);
                if (_authTokens.ContainsKey(request.ApplicationLoginID)) _authTokens[request.ApplicationLoginID] = authReponse.Token;
                else _authTokens.Add(request.ApplicationLoginID, authReponse.Token);
                return new Tuple<AuthorizeResponse, Error>(authReponse, null);
            }
            else
            {
                _logger.LogError($"Payten response error: {response.ErrorMessage}");
                return new Tuple<AuthorizeResponse, Error>(null, new Error { description = "Neuspjela autorizacija!" });
            }
        }


        public async Task<Tuple<CreatePaymentSessionTokenResponse, Error>> CreatePaymentSessionToken(CreatePaymentSessionTokenRequest request, string token)
        {
            var restRequest = new RestRequest("/createpaymentsessiontoken", Method.Post).AddJsonBody(request);
            restRequest.AddHeader("Authorization", $"Bearer {token}");

            var response = await _client.ExecutePostAsync(restRequest);

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var tokenReponse = JsonSerializer.Deserialize<CreatePaymentSessionTokenResponse>(response.Content);
                return new Tuple<CreatePaymentSessionTokenResponse, Error>(tokenReponse, null);
            }
            else
            {
                _logger.LogError($"Payten response error: {response.ErrorMessage}");
                return new Tuple<CreatePaymentSessionTokenResponse, Error>(null, new Error { description = "Neuspjela sesija!" });
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

        public string DecryptPayload(string base64Encrypted, string key)
        {
            // Convert key and base64 payload
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] fullCipher = Convert.FromBase64String(base64Encrypted);

            // Validate key length
            if (keyBytes.Length != 32)
                throw new ArgumentException("Key must be 32 bytes long (256-bit AES).");

            // Extract IV and cipher
            byte[] iv = new byte[16];
            byte[] cipherBytes = new byte[fullCipher.Length - 16];
            Array.Copy(fullCipher, iv, 16);
            Array.Copy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);

            // Decrypt
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            var json = Encoding.UTF8.GetString(plainBytes);

            if (json.StartsWith("\":"))
            {
                json = "{ \"Transaction " + json + " }";
            }

            return json;
        }
    }
}
