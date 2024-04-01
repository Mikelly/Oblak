using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Municipality
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
                
        [StringLength(450)]
        public Country Country { get; set; }

        [StringLength(450)]
        public string Name { get; set; }
                
        [StringLength(450)]
        public string? ExternalId { get; set; }

        public string Address { get; set; }

        public decimal ResidenceTaxAmount { get; set; }

        [StringLength(450)]
        public string ResidenceTaxAccount { get; set; }

        public List<Property> Properties { get; set; }
    }

}
