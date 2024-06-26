namespace Oblak.Models.Api
{
    public class GroupPaymentInfoDto
    {
        public string? MerchantTransactionId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? PaymentResponse { get; set; }
    }
}
