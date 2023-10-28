using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class CodeList
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  
        public int Id { get; set; }

        [StringLength(450)]
        public string Country { get; set; }

        [StringLength(450)]
        public string ExternalId { get; set; }

        [StringLength(450)]
        public string Type { get; set; }

        [StringLength(450)]
        public string Name { get; set; }
                
        [StringLength(450)]
        public string? Param1 { get; set; }

        [StringLength(450)]
        public string? Param2 { get; set; }

        [StringLength(450)]
        public string? Param3 { get; set; }

        [StringLength(8000)]
        public string? Base64Data { get; set; }
    }
}
