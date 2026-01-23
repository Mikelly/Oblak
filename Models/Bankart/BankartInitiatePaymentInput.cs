namespace Oblak.Models.Bankart;

public sealed class BankartInitiatePaymentInput
{
    public int GroupId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ReferenceUuid { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string ErrorUrl { get; set; } = string.Empty;
    public bool StoreCard { get; set; } = false;
}
