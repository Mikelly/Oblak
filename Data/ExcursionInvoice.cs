using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ExcursionInvoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
                
        public int AgencyId { get; set; }

        public int CheckInPointId { get; set; }

        public int InvoiceNo { get; set; }

        [StringLength(450)]
        public string InvoiceNumber { get; set; }

        public DateTime Date { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? BillingPeriodFrom { get; set; }

        public DateTime? BillingPeriodTo { get; set; }

        [StringLength(450)]
        public string? BillingNote { get; set; }

        [StringLength(450)]
        public string? Note { get; set; }

        public int TaxPaymentTypeId { get; set; }

        public decimal BillingAmount { get; set; }

        public decimal BillingFee { get; set; }

        [StringLength(450)]
        public TaxInvoiceStatus Status { get; set; }


        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion


        public Agency Agency { get; set; }

        public CheckInPoint CheckInPoint { get; set; }

        public TaxPaymentType TaxPaymentType { get; set; }

        public List<ExcursionInvoiceItem> ExcursionInvoiceItems { get; set; }
    }
}