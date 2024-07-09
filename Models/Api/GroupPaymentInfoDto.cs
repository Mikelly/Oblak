namespace Oblak.Models.Api
{
    public class GroupPaymentInfoDto
    {
        public string? MerchantTransactionId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? TotalAmount { get; set; } // Mobile app needs to be updated to use this property instead of PaymentResponse
        public string? Amount { get; set; } // Mobile app needs to be updated to use this property instead of PaymentResponse
        public string? Surcharge { get; set; } // Mobile app needs to be updated to use this property instead of PaymentResponse
        public string? Currency { get; set; } // Mobile app needs to be updated to use this property instead of PaymentResponse
        public string? AuthCode { get; set; } // Mobile app needs to be updated to use this property instead of PaymentResponse
        public string? CardType { get; set; } // Mobile app needs to be updated to use this property instead of PaymentResponse
        public string? LastFourDigits { get; set; } // Mobile app needs to be updated to use this property instead of PaymentResponse
        public string? PaymentResponse { get; set; } // Remove this proprety after the mobile app is updated to use TotalAmount, Amount, Surcharge, Currency, AuthCode, CardType, LastFourDigits
    }
}
