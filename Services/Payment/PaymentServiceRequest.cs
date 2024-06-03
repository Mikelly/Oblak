namespace Oblak.Services.Payment
{
    public class PaymentServiceRequest
    {
        public string MerchantTransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal SurchargeAmount { get; set; }
        public string TransactionToken { get;set; } = string.Empty;
    }
}
