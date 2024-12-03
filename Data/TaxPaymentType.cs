using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
	public class TaxPaymentType
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int PartnerId { get; set; }

		public bool IsBalanced { get; set; } = false;

        public bool IsCash { get; set; } = false;

        public TaxPaymentStatus TaxPaymentStatus { get; set; }

        [StringLength(450)]
        public string Description { get; set; }

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

				
		public Partner Partner { get; set; }

		public List<ExcursionInvoice> ExcursionInvoices { get; set; }	
	}
}
