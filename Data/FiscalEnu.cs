using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public partial class FiscalEnu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id  { get; set; }

        [StringLength(450)]
        public string Code { get; set; }

        public int LegalEntityId { get; set; }        

        public int No { get; set; }

        [StringLength(450)]
        public string? Type  { get; set; }

        public decimal? AutoDeposit { get; set; } = 0m;

        public string? Settings { get; set; } = string.Empty;

        public string Status { get; set; } = "A";


        #region Payteon

        public string? PayteonId { get; set; }
        public string? PayteonPassword { get; set; }
        public string? PayteonUser { get; set; }

        #endregion


        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
        
        #endregion


        #region Navigation Properties

        public LegalEntity LegalEntity { get; set; }

        #endregion
    }
}
