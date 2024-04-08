namespace Oblak.Models.Api
{
    public class InitiatePosPaymentSessionInput
    {
        public int DocumentId { get; set; }
        public string TransactionType { get; set; }
    }
}
