using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class ItemDto
    {
        public int Id { get; set; }

        public int LegalEntityId { get; set; }
                
        public string Code { get; set; }
                
        public string Name { get; set; }
                
        public string? Description { get; set; }
        
        public string Unit { get; set; }

        public decimal Price { get; set; }

        public decimal VatRate { get; set; }

        public bool PriceInclVat { get; set; }
        
        public string? VatExempt { get; set; } = null;
    }
}
