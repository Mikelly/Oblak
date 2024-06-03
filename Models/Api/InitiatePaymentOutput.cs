namespace Oblak.Models.Api
{
    public class InitiatePaymentOutput
    {
        public string RedirectUrl { get; set; } = string.Empty;
        public string RedirectType { get; set; } = string.Empty;
    }
}
