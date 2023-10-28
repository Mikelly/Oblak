using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ResTax
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int LegalEntityId { get; set; }

        public LegalEntity LegalEntity { get; set; }
        
        public int PropertyRefId { get; set; }
        
        public int PropertyId { get; set; }
        
        public Property Property { get; set; }
        
        public DateTime Date { get; set; }
        
        public DateTime DateFrom { get; set; }
        
        public DateTime DateTo { get; set; }
        
        public decimal Amount { get; set; }
        
        [StringLength(450)]        
        public string Status { get; set; }

        public List<ResTaxItem> Items { get; set; }
    }
}