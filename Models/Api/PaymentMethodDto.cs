namespace Oblak.Models.Api
{
    public class PaymentMethodDto
    {
        public string ReferenceUuid { get; set; }
        public string Type { get; set; }
        public string LastFourDigits { get; set; }
    }
}
