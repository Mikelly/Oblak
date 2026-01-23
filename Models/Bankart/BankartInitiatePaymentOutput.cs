namespace Oblak.Models.Bankart;

public sealed class BankartInitiatePaymentOutput
{
    public string RedirectUrl { get; set; } = string.Empty;
    public string RedirectType { get; set; } = string.Empty;
}
