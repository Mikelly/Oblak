using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Data
{
    public class ResTaxExemptionType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PartnerId { get; set; }                

        [StringLength(450)]
        public string Description { get; set; }

        [StringLength(450)]
        public string Status { get; set; } = "A";


        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]
        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }

        #endregion


        #region Navigation Properties

        public Partner Partner { get; set; }

        #endregion
    }
}
