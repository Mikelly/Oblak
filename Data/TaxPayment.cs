using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
	public class TaxPayment
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int? LegalEntityId { get; set; }

        public int? AgencyId { get; set; }

        public int TaxPaymentTypeId { get; set; }

		public TaxType TaxType { get; set; }

        public DateTime TransactionDate { get; set; }

		public decimal Amount { get; set; }

		[StringLength(450)]
        public string? PaymentRef { get; set; }

        [StringLength(450)]
        public string? Note { get; set; }


        #region Audit Properties

        [StringLength(450)]
		public string? UserCreated { get; set; }

		public DateTime? UserCreatedDate { get; set; }

		[StringLength(450)]
		public string? UserModified { get; set; }

		public DateTime? UserModifiedDate { get; set; }

		#endregion


		
		public LegalEntity? LegalEntity { get; set; }

        public Agency? Agency { get; set; }

        public TaxPaymentType? TaxPaymentType { get; set; }
    }
}
