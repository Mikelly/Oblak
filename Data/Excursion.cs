using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Excursion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
                
        public int AgencyId { get; set; }
                
        public DateTime Date { get; set; }

        [StringLength(450)]
        public string VoucherNo { get; set; }

        public int? CountryId { get; set; }

        public int NoOfPersons { get; set; }

        [StringLength(450)]
        public string? ExcursionTaxExempt { get; set; }

        public decimal ExcursionTaxAmount { get; set; }
        

        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion


        public Agency Agency { get; set; }

        public Country? Country { get; set; }
    }
}