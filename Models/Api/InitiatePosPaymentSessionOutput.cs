namespace Oblak.Models.Api
{
    public class InitiatePosPaymentSessionOutput
    {
        public string PaymentSessionToken { get; set; }
        public int TransactionId { get; set; }
    }
}
