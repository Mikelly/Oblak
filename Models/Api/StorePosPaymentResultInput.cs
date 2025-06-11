namespace Oblak.Models.Api
{
    public class StorePosPaymentResultInput
    {
        public int TransactionId { get; set; }
        public DateTime? TransactionDate { get; set; } = DateTime.UtcNow;
        public string? Status { get; set; }
        public bool Success { get; set; }
        public string? JsonResult { get; set; }
    }
}
