namespace Oblak.Services.Bankart;

public interface IBankartService
{
    string GetConfigurationValue(bool test, string key);
    Task<BankartServiceResponse> InitiatePaymentAsync(BankartServiceRequest input);
    Task<BankartServiceResponse> PreauthorizeTransactionAsync(BankartServiceRequest input);
    Task<BankartServiceResponse> VoidTransactionAsync(BankartServiceRequest input);
    Task<BankartServiceResponse> DeregisterPaymentMethodAsync(BankartServiceRequest input);
    bool ValidateWebhookSignature(string requestBody, string requestUri, string dateHeader, string xSignatureHeader, bool testMode);
}
