using Newtonsoft.Json;
using Oblak.Data.Enums;
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
        }

        public string GetConfigurationValue(bool testMode, string key)
        {
            return testMode
                ? _configuration[$"Payments:TEST:{key}"]!
                : _configuration[$"Payments:PROD:{key}"]!;
        }

        public async Task<PaymentServiceResponse> InitiatePaymentAsync(PaymentServiceRequest input)
        {
            _client = new RestClient(GetConfigurationValue(input.TestMode, "Endpoint"));

            var requestBody = new Dictionary<string, object>
            {
                { "merchantTransactionId", input.MerchantTransactionId },
                { "amount", string.Format(CultureInfo.InvariantCulture, "{0:F2}", input.Amount) },
                { "surchargeAmount", string.Format(CultureInfo.InvariantCulture, "{0:F2}", input.SurchargeAmount) },
                { "currency", "EUR" },
                { "successUrl", input.SuccessUrl },
                { "cancelUrl", input.CancelUrl },
                { "errorUrl", input.ErrorUrl },
                { "callbackUrl", GetConfigurationValue(input.TestMode, "Callback") }
            };

            if (string.IsNullOrEmpty(input.ReferenceUuid))
            {
                requestBody.Add("transactionToken", input.TransactionToken);
                requestBody.Add("withRegister", input.WithRegister);
            }
            else
            {
                requestBody.Add("referenceUuid", input.ReferenceUuid);
                requestBody.Add("withRegister", false);
                requestBody.Add("transactionIndicator", "CARDONFILE");
            }

            var apiKey = GetConfigurationValue(input.TestMode, "ApiKey");
            var secret = GetConfigurationValue(input.TestMode, "SharedSecret");
            var dateHeader = DateTime.UtcNow.ToString("R");

            var jsonBody = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });
            var bodyHash = ComputeSHA512Hash(jsonBody);

            var message = $"POST\n{bodyHash}\napplication/json; charset=utf-8\n{dateHeader}\n/api/v3/transaction/{apiKey}/debit";
            var signature = GenerateSignature(secret, message);

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetConfigurationValue(input.TestMode, "ApiUser") + ":" + GetConfigurationValue(input.TestMode, "Password")));

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
                    ReturnType = PaymentResponseTypes.ERROR.ToString(),
                    Errors = [new PaymentError { ErrorMessage = response.ErrorMessage }] 
                };
            }
        }

        public async Task<PaymentServiceResponse> RegisterPaymentMethod(PaymentServiceRequest input)
        {
            _client = new RestClient(GetConfigurationValue(input.TestMode, "Endpoint"));

            var requestBody = new
            {
                merchantTransactionId = input.MerchantTransactionId,
                successUrl = input.SuccessUrl,
                cancelUrl = input.CancelUrl,
                errorUrl = input.ErrorUrl,
                callbackUrl = GetConfigurationValue(input.TestMode, "Callback"),
                transactionToken = input.TransactionToken
            };

            var apiKey = GetConfigurationValue(input.TestMode, "ApiKey");
            var secret = GetConfigurationValue(input.TestMode, "SharedSecret");
            var dateHeader = DateTime.UtcNow.ToString("R");

            var jsonBody = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });
            var bodyHash = ComputeSHA512Hash(jsonBody);

            var message = $"POST\n{bodyHash}\napplication/json; charset=utf-8\n{dateHeader}\n/api/v3/transaction/{apiKey}/register";
            var signature = GenerateSignature(secret, message);

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetConfigurationValue(input.TestMode, "ApiUser") + ":" + GetConfigurationValue(input.TestMode, "Password")));

            var request = new RestRequest($"api/v3/transaction/{apiKey}/register", Method.Post);

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
                return new PaymentServiceResponse
                {
                    Success = false,
                    ReturnType = PaymentResponseTypes.ERROR.ToString(),
                    Errors = [new PaymentError { ErrorMessage = response.ErrorMessage }]
                };
            }
        }

        public async Task<PaymentServiceResponse> DeregisterPaymentMethod(PaymentServiceRequest input)
        {
            _client = new RestClient(GetConfigurationValue(input.TestMode, "Endpoint"));

            var requestBody = new
            {
                merchantTransactionId = input.MerchantTransactionId,
                referenceUuid = input.ReferenceUuid,
                callbackUrl = GetConfigurationValue(input.TestMode, "Callback")
            };

            var apiKey = GetConfigurationValue(input.TestMode, "ApiKey");
            var secret = GetConfigurationValue(input.TestMode, "SharedSecret");
            var dateHeader = DateTime.UtcNow.ToString("R");

            var jsonBody = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });
            var bodyHash = ComputeSHA512Hash(jsonBody);

            var message = $"POST\n{bodyHash}\napplication/json; charset=utf-8\n{dateHeader}\n/api/v3/transaction/{apiKey}/deregister";
            var signature = GenerateSignature(secret, message);

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetConfigurationValue(input.TestMode, "ApiUser") + ":" + GetConfigurationValue(input.TestMode, "Password")));

            var request = new RestRequest($"api/v3/transaction/{apiKey}/deregister", Method.Post);

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
                return new PaymentServiceResponse
                {
                    Success = false,
                    ReturnType = PaymentResponseTypes.ERROR.ToString(),
                    Errors = [new PaymentError { ErrorMessage = response.ErrorMessage }]
                };
            }
        }

        public bool ValidateSignature(string requestBody, string requestUri, string dateHeader, string xSignatureHeader, bool testMode)
        {
            var bodyHash = ComputeSHA512Hash(requestBody);
            var message = $"POST\n{bodyHash}\napplication/json; charset=utf-8\n{dateHeader}\n{requestUri}";
            var secret = GetConfigurationValue(testMode, "SharedSecret");
            var computedSignature = GenerateSignature(secret, message);

            return computedSignature == xSignatureHeader;
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
