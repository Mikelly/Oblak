using Oblak.Data.Enums;

namespace Oblak.Models.Api
{
	public class TaxPaymentBalanceDto
	{
		public int Id { get; set; }
		public int LegalEntityId { get; set; }
		public TaxType TaxType { get; set; }
		public decimal Balance { get; set; }
	}
}
