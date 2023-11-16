using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class DocumentItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int DocumentId { get; set; }        
        
        public int ItemId { get; set; }

        [StringLength(450)]
        public string ItemCode { get; set; }
        
        [StringLength(450)]
        public string ItemName { get; set; }
        
        [StringLength(450)]
        public string ItemUnit { get; set; }
        
        public MneVatExempt? VatExempt { get; set; }
        
        public decimal Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        public decimal UnitPriceWoVat { get; set; }
        
        public decimal LineAmount { get; set; }
        
        public decimal Discount { get; set; }
        
        public decimal DiscountAmount { get; set; }
                
        public decimal VatRate { get; set; }
        
        public decimal VatAmount { get; set; }
        
        public decimal FinalPrice { get; set; }
        
        public decimal LineTotalWoVat { get; set; }
        
        public decimal LineTotal { get; set; }



        public Item Item { get; set; }
        public Document Document { get; set; }

    }
}
