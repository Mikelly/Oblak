using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Data.Api
{
    public class VesselDto
    {
        public int Id { get; set; }

        public int PartnerId { get; set; }

        public int? LegalEntityId { get; set; }
        public int? CountryId { get; set; }

        public string? LegalEntityName { get; set; }

        public string? CountryName { get; set; }

        public string VesselType { get; set; }
                
        public string Name { get; set; }
                
        public string Registration { get; set; }

        public decimal Length { get; set; }
                
        public string OwnerName { get; set; }
                
        public string? OwnerAddress { get; set; }

        [Required]
        public string? OwnerTIN { get; set; }
                
        public string? OwnerPhone { get; set; }
                
        public string? OwnerEmail { get; set; }
    }
}
