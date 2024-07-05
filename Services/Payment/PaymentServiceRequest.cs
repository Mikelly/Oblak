namespace Oblak.Services.Payment
{
    public class PaymentServiceRequest
    {
        public string MerchantTransactionId { get; set; } = string.Empty;
        public string ReferenceUuid { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal SurchargeAmount { get; set; }
        public string TransactionToken { get;set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string ErrorUrl { get; set; } = string.Empty;
        public bool WithRegister { get; set; } = false;
        public bool TestMode { get; set; } = true;
    }
}
