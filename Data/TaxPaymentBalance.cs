using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
	public class TaxPaymentBalance
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int? LegalEntityId { get; set; }

        public int? AgencyId { get; set; }

        public TaxType TaxType { get; set; }

		public decimal Balance { get; set; }


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
    }
}
