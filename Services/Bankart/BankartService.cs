using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Oblak.Services.Bankart;

public sealed class BankartService(IConfiguration configuration, ILogger<BankartService> logger) : IBankartService
{
    private const string currencyEur = "EUR";
    private const string transactionIndicatorSingle = "SINGLE";
    private const string transactionIndicatorCardOnFile = "CARDONFILE";

    public string GetConfigurationValue(bool test, string key)
    {
        return test
            ? configuration[$"BANKART:TEST:{key}"]!
            : configuration[$"BANKART:PROD:{key}"]!;
    }

    public async Task<BankartServiceResponse> InitiatePaymentAsync(BankartServiceRequest input)
    {
        var requestBody = BuildBaseRequestBody(input);

        requestBody.Add("surchargeAmount", string.Format(CultureInfo.InvariantCulture, "{0:F2}", input.SurchargeAmount));

        if (string.IsNullOrEmpty(input.ReferenceUuid))
        {
            requestBody.Add("transactionToken", input.TransactionToken);
            requestBody.Add("withRegister", input.WithRegister);
            requestBody.Add("transactionIndicator", transactionIndicatorSingle);
        }
        else
        {
            requestBody.Add("referenceUuid", input.ReferenceUuid);
            requestBody.Add("withRegister", false);
            requestBody.Add("transactionIndicator", transactionIndicatorCardOnFile);
        }

        return await ExecutePaymentActionAsync(input, requestBody, BankartActionType.Debit);
    }

    public async Task<BankartServiceResponse> PreauthorizeTransactionAsync(BankartServiceRequest input)
    {
        var requestBody = BuildBaseRequestBody(input);

        requestBody.Add("transactionToken", input.TransactionToken);
        requestBody.Add("withRegister", true);
        requestBody.Add("transactionIndicator", transactionIndicatorSingle);

        return await ExecutePaymentActionAsync(input, requestBody, BankartActionType.Preauthorize);
    }

    public async Task<BankartServiceResponse> VoidTransactionAsync(BankartServiceRequest input)
    {
        var requestBody = new
        {
            merchantTransactionId = input.MerchantTransactionId,
            referenceUuid = input.ReferenceUuid
        };

        return await ExecutePaymentActionAsync(input, requestBody, BankartActionType.Void);
    }

    public async Task<BankartServiceResponse> DeregisterPaymentMethodAsync(BankartServiceRequest input)
    {
        var requestBody = new
        {
            merchantTransactionId = input.MerchantTransactionId,
            referenceUuid = input.ReferenceUuid
        };

        return await ExecutePaymentActionAsync(input, requestBody, BankartActionType.Deregister);
    }

    public bool ValidateWebhookSignature(string requestBody, string requestUri, string dateHeader, string xSignatureHeader, bool testMode)
    {
        var bodyHash = ComputeSha512Hash(requestBody);

        var message = $"POST\n{bodyHash}\napplication/json; charset=utf-8\n{dateHeader}\n{requestUri}";

        var secret = GetConfigurationValue(testMode, "SharedSecret");

        var computedSignature = GenerateSignature(secret, message);

        return computedSignature == xSignatureHeader;
    }

    private Dictionary<string, object> BuildBaseRequestBody(BankartServiceRequest input)
    {
        return new Dictionary<string, object>
        {
            { "merchantTransactionId", input.MerchantTransactionId },
            { "amount", string.Format(CultureInfo.InvariantCulture, "{0:F2}", input.Amount) },
            { "currency", currencyEur },
            { "successUrl", input.SuccessUrl },
            { "cancelUrl", input.CancelUrl },
            { "errorUrl", input.ErrorUrl },
            { "callbackUrl", GetConfigurationValue(input.TestMode, "Callback") },
            { "customer", new Dictionary<string, object> {
                { "firstName", input.FirstName },
                { "lastName", input.LastName },
                { "billingAddress1", input.BillingAddress1 },
                { "identification", input.Identification },
                { "email", input.Email },
                { "billingCountry", GetConfigurationValue(input.TestMode, "BillingCountry") },
                { "billingCity", GetConfigurationValue(input.TestMode, "BillingCity") },
                { "billingPostcode", GetConfigurationValue(input.TestMode, "BillingPostalCode") }
            } },
            { "extraData", new Dictionary<string, object> {
                { "billPayAccount", input.Identification },
                { "insuranceType", "UNIQA" }
            } }
        };
    }

    private async Task<BankartServiceResponse> ExecutePaymentActionAsync(BankartServiceRequest input, object requestBody, string actionType)
    {
        var apiKey = GetConfigurationValue(input.TestMode, "ApiKey");
        var secret = GetConfigurationValue(input.TestMode, "SharedSecret");

        var dateHeader = DateTime.UtcNow.ToString("R");

        var jsonBody = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var bodyHash = ComputeSha512Hash(jsonBody);

        var message = $"POST\n{bodyHash}\napplication/json; charset=utf-8\n{dateHeader}\n/api/v3/transaction/{apiKey}/{actionType}";

        var signature = GenerateSignature(secret, message);

        var username = GetConfigurationValue(input.TestMode, "ApiUser");
        var password = GetConfigurationValue(input.TestMode, "Password");

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));

        var request = new RestRequest($"api/v3/transaction/{apiKey}/{actionType}", Method.Post);

        request.AddHeader("Content-Type", "application/json; charset=utf-8");
        request.AddHeader("Date", dateHeader);
        request.AddHeader("Authorization", $"Basic {authHeader}");
        request.AddHeader("X-Signature", signature);
        request.AddJsonBody(jsonBody);

        var client = new RestClient(GetConfigurationValue(input.TestMode, "Endpoint"));

        var response = await client.ExecuteAsync(request);

        if (response.IsSuccessful && response.Content is not null)
        {
            return JsonConvert.DeserializeObject<BankartServiceResponse>(response.Content)!;
        }
        else
        {
            var transactionId = input.MerchantTransactionId;

            var errorMessage = response.ErrorMessage;

            var errorException = response.ErrorException;

            logger.LogError("Payment request failed {transactionId}: {errorMessage}\n{errorException}",
                transactionId, errorMessage, errorException);

            return BankartServiceResponse.Failure([new PaymentError { ErrorMessage = errorMessage }]);
        }
    }

    private static string ComputeSha512Hash(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));

        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string GenerateSignature(string secret, string message)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));

        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));

        return Convert.ToBase64String(hash);
    }
}
