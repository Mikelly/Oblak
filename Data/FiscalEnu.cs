using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public partial class FiscalEnu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id  { get; set; }

        public int LegalEntityId { get; set; }

        public int PropertyId { get; set; }

        [StringLength(450)]
        public string FiscalEnuCode { get; set; }

        [StringLength(450)]
        public string OperatorCode { get; set; }

        public int No { get; set; }

        [StringLength(450)]
        public string? Type  { get; set; }

        public decimal? AutoDeposit { get; set; } = 0m;

        [StringLength(450)]
        public string? Settings { get; set; } = string.Empty;

        [StringLength(450)]
        public string Status { get; set; } = "A";


        #region Payteon

        [StringLength(450)]
        public string? PayteonId { get; set; }

        [StringLength(450)]
        public string? PayteonPassword { get; set; }

        [StringLength(450)]
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

        public Property Property { get; set; }

        #endregion
    }
}
