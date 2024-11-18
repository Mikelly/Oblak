using System;
using Oblak.Data.Enums;

namespace Oblak.Models.Api
{
    public class ExcursionInvoiceDto
    {
        public int Id { get; set; }

        public int AgencyId { get; set; }

        public string AgencyName { get; set; }

        public int InvoiceNo { get; set; }

        public string InvoiceNumber { get; set; }

        public DateTime Date { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? BillingPeriodFrom { get; set; }

        public DateTime? BillingPeriodTo { get; set; }

        public string? BillingNote { get; set; }

        public string? Note { get; set; }

        public int TaxPaymentTypeId { get; set; }

        public string TaxPaymentTypeDescription { get; set; }

        public decimal BillingAmount { get; set; }

        public decimal BillingFee { get; set; }

        public decimal BillingTotal { get; set; }

        public string Status { get; set; }



        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
    }
}
