namespace Oblak.Services.Bankart;

public sealed class BankartServiceRequest
{
    public string MerchantTransactionId { get; set; } = string.Empty;
    public string ReferenceUuid { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public string TransactionToken { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string ErrorUrl { get; set; } = string.Empty;
    public bool WithRegister { get; set; } = false;
    public bool TestMode { get; set; } = true;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string BillingAddress1 { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
