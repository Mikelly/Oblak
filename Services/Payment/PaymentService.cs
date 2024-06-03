using Newtonsoft.Json;
using RestSharp;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Oblak.Services.Payment
{
    public class PaymentService
    {
        ILogger<PaymentService> _logger;
        IConfiguration _configuration;
        RestClient _client;

        public PaymentService(ILogger<PaymentService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new RestClient(_configuration["Payments:Endpoint"]!);
        }

        public async Task<PaymentServiceResponse> InitiatePaymentAsync(PaymentServiceRequest input)
        {
            var requestBody = new
            {
                merchantTransactionId = input.MerchantTransactionId,
                amount = string.Format(CultureInfo.InvariantCulture, "{0:F2}", input.Amount),
                surchargeAmount = string.Format(CultureInfo.InvariantCulture, "{0:F2}", input.SurchargeAmount),
                currency = "EUR",
                successUrl = _configuration["Payments:SuccessUrl"]!,
                cancelUrl = _configuration["Payments:CancelUrl"]!,
                errorUrl = _configuration["Payments:ErrorUrl"]!,
                callbackUrl = _configuration["Payments:Callback"]!,
                transactionToken = input.TransactionToken
            };

            var apiKey = _configuration["Payments:ApiKey"]!;
            var secret = _configuration["Payments:SharedSecret"]!;
            var dateHeader = DateTime.UtcNow.ToString("R");

            var jsonBody = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });
            var bodyHash = ComputeSHA512Hash(jsonBody);

            var message = $"POST\n{bodyHash}\napplication/json; charset=utf-8\n{dateHeader}\n/api/v3/transaction/{apiKey}/debit";
            var signature = GenerateSignature(secret, message);

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(_configuration["Payments:ApiUser"] + ":" + _configuration["Payments:Password"]));

            var request = new RestRequest($"api/v3/transaction/{apiKey}/debit", Method.Post);

            request.AddHeader("Content-Type", "application/json; charset=utf-8");
            request.AddHeader("Date", dateHeader);
            request.AddHeader("Authorization", $"Basic {authHeader}");
            request.AddHeader("X-Signature", signature);
            request.AddJsonBody(jsonBody);

            var response = await _client.ExecuteAsync(request);

            if (response.IsSuccessful && response.Content != null)
            {
                return JsonConvert.DeserializeObject<PaymentServiceResponse>(response.Content)!;
            }
            else
            {
                var transactionId = input.MerchantTransactionId;
                var errorMessage = response.ErrorMessage;
                var errorException = response.ErrorException;
                _logger.LogError("Payment request failed {transactionId}: {errorMessage}\n{errorException}", transactionId, errorMessage, errorException);
                return new PaymentServiceResponse { 
                    Success = false, 
                    Errors = [new PaymentError { ErrorMessage = response.ErrorMessage }] 
                };
            }
        }

        private static string ComputeSHA512Hash(string input)
        {
            using var sha512 = SHA512.Create();
            var bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        private static string GenerateSignature(string secret, string message)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hash);
        }
    }
}
