namespace Oblak.Models.Payment;

public class PaymentTransactionViewModel
{
    public int Id { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public string? Status { get; set; }
    public bool? Success { get; set; }
    public DateTime UserCreatedDate { get; set; }
}
