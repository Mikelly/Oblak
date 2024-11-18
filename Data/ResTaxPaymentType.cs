using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ResTaxPaymentType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int PartnerId { get; set; }

        public Partner Partner { get; set; }

        [StringLength(450)]
        public string Description { get; set; }

        [StringLength(450)]
		public ResTaxPaymentStatus PaymentStatus { get; set; }
        
        [StringLength(450)]        
        public string Status { get; set; }

		[StringLength(450)]
		public string? UserCreated { get; set; }

		public DateTime? UserCreatedDate { get; set; }

		[StringLength(450)]
		public string? UserModified { get; set; }

		public DateTime? UserModifiedDate { get; set; }

		public List<ResTaxFee> PaymentFees { get; set; }

	}
}