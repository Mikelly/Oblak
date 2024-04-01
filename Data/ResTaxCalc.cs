using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ResTaxCalc
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int LegalEntityId { get; set; }

        public LegalEntity LegalEntity { get; set; }
        
        public int PropertyId { get; set; }
        
        public Property Property { get; set; }
        
        public DateTime Date { get; set; }
        
        public DateTime DateFrom { get; set; }
        
        public DateTime DateTo { get; set; }
        
        public decimal Amount { get; set; }
        
        [StringLength(450)]        
        public string Status { get; set; }

		[StringLength(450)]
		public string? UserCreated { get; set; }

		public DateTime? UserCreatedDate { get; set; }

		[StringLength(450)]
		public string? UserModified { get; set; }

		public DateTime? UserModifiedDate { get; set; }

		public List<ResTaxCalcItem> Items { get; set; }
    }
}