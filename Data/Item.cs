using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int LegalEntityId { get; set; }               
       
        [StringLength(450)]
        public string Code { get; set; }
        
        [StringLength(450)]        
        public string Name { get; set; }
        
        [StringLength(450)]
        public string Description { get; set; }
        
        [StringLength(450)]
        public string Unit { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal VatRate { get; set; }
        
        public bool PriceInclVat { get; set; }

        [StringLength(450)]
        public string VatExempt { get; set; }


        public LegalEntity LegalEntity { get; set; }
    }
}
