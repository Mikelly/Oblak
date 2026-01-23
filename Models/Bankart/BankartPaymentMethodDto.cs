namespace Oblak.Models.Bankart;

public sealed class BankartPaymentMethodDto
{
    public string ReferenceUuid { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
}
