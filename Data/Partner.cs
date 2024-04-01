using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
    public class Partner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(450)]
        public Country Country { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string? TIN { get; set; }

        [StringLength(450)]
        public PartnerType? PartnerType { get; set; }

        [StringLength(2000)]
        public string? Address { get; set; }

        [StringLength(450)]
        public string? ResidenceTaxAccount { get; set; }

        [StringLength(450)]
        public string? ResidenceTaxName { get; set; }

        [StringLength(450)]
        public string? ResidenceTaxDescription { get; set; }

        public byte[]? Logo { get; set; }

        [StringLength(450)]
        public string? Reference { get; set; } = Guid.NewGuid().ToString();

        public List<LegalEntity> LegalEntities { get; set; }


        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion
    }
}