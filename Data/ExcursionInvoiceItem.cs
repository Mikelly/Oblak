using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ExcursionInvoiceItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
                
        public int ExcursionInvoiceId { get; set; }

        public DateTime Date { get; set; }

        public string? VoucherNo { get; set; }

        public string? TaxExempt { get; set; }

        public int NoOfPersons { get; set; }

        public decimal Price { get; set; }

        public decimal Amount { get; set; }

        [StringLength(450)]
        public string? Note { get; set; }

        
        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion


        public ExcursionInvoice ExcursionInvoice { get; set; }

    }
}