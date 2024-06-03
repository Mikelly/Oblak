namespace Oblak.Services.Payment
{
    public class PaymentServiceResponse
    {
        public bool Success { get; set; } = false;
        public string Uuid { get; set; } = string.Empty;
        public string PurchaseId { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string RedirectType { get; set; } = string.Empty;
        public List<PaymentError>? Errors { get; set; }
    }

    public class PaymentError
    {
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public string? AdapterMessage { get; set; }
        public string? AdapterCode { get; set; }
    }
}
