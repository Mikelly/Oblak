using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class PropertyUnit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int LegalEntityId { get; set; }        
        
        public int PropertyId { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string? FloorNo { get; set; }

        [StringLength(450)]
        public string? RoomNo { get; set; }

        public decimal Price { get; set; }

        [StringLength(450)]
        public string? Naplata { get; set; }
        
        public int? ItemId { get; set; }


        public LegalEntity LegalEntity { get; set; }
    }
}
