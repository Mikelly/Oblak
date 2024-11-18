using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ResTaxFee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int PartnerId { get; set; }

        public Partner Partner { get; set; }

        [StringLength(450)]
        public string Description { get; set; }
        
        public int ResTaxPaymentTypeId { get; set; }
                
        public ResTaxPaymentType ResTaxPaymentType {  get; set; } 

        public decimal? FeeAmount { get; set; }

		public decimal? FeePercentage { get; set; }

		public decimal LowerLimit { get; set; }

		public decimal UpperLimit {  get; set; }

		[StringLength(450)]
		public string? UserCreated { get; set; }

		public DateTime? UserCreatedDate { get; set; }

		[StringLength(450)]
		public string? UserModified { get; set; }

		public DateTime? UserModifiedDate { get; set; }		
    }
}