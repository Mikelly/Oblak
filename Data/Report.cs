using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string Path { get; set; }

        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]
        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }

    }
}
