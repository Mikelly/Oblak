using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ResTaxType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int PartnerId { get; set; }

        public Partner Partner { get; set; }

        [StringLength(450)]
        public string Description { get; set; }

        public decimal Amount { get; set; }

        public int? AgeFrom { get; set; }

        public int? AgeTo { get; set; }

        [StringLength(450)]        
        public string Status { get; set; }

		[StringLength(450)]
		public string? UserCreated { get; set; }

		public DateTime? UserCreatedDate { get; set; }

		[StringLength(450)]
		public string? UserModified { get; set; }

		public DateTime? UserModifiedDate { get; set; }		
    }
}