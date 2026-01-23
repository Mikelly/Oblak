namespace Oblak.Models.Bankart;

public sealed class BankartRegisterPaymentMethodInput
{
    public string Token { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string ErrorUrl { get; set; } = string.Empty;
}
