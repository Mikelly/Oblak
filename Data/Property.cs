using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
    public class Property
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int ExternalId { get; set; }
        
        public int LegalEntityId { get; set; }

        [JsonIgnore]
        public LegalEntity LegalEntity { get; set; }

        [StringLength(450)]
        public string? RegNumber { get; set; }
        
        public DateTime? RegDate { get; set; }

        [StringLength(450)]
        public string? Type { get; set; }

        [StringLength(450)]
        public string? Code { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string PropertyName { get; set; }

        [StringLength(450)]
        public string? Address { get; set; }

        [StringLength(450)]
        public string? Place { get; set; }

        [StringLength(450)]
        public string? Municipality { get; set; }
        
        public decimal? GeoLon { get; set; }
        
        public decimal? GeoLat { get; set; }
        
        public decimal? Price { get; set; }

        [StringLength(450)]
        public string? PaymentType { get; set; }
        
        public decimal? ResidenceTax { get; set; }
        
        // samo za MNE klijenta
        public bool? ResidenceTaxYN { get; set; }
        
        public int? Capacity { get; set; }

        [StringLength(450)]
        public string Status { get; set; } = "A";

        // samo za MNE klijenta
        [StringLength(450)]
        public string? BusinessUnitCode { get; set; }

        // samo za MNE klijenta
        [StringLength(450)]
        public string? FiscalEnuCode { get; set; }

        [StringLength(450)]
        public string? DefaultEntryPoint { get; set; }

        public bool AutoCheckOut { get; set; }
        
        public TimeSpan? AutoCheckOutTime { get; set; }

        [StringLength(450)]
        public string? UserCreated { get; set; }
        
        public DateTime? UserCreatedDate { get; set; }
        
        [StringLength(450)]
        public string? UserModified { get; set; }
        
        public DateTime? UserModifiedDate { get; set; }

        [JsonIgnore]
        public List<Group> Groups { get; set; }
        [JsonIgnore]
        public List<Person> Persons { get; set; }
        public List<SelfRegisterToken> GuestTokens { get; set; }
        public List<ResTax> ResTaxes { get; set; }
    }
}