using System;

namespace Oblak.Models.Api
{
    public class ExcursionInvoiceItemDto
    {
        public int Id { get; set; }

        public int ExcursionInvoiceId { get; set; }

        public DateTime Date { get; set; }

        public string? VoucherNo { get; set; }

        public string? TaxExempt { get; set; }

        public int NoOfPersons { get; set; }

        public decimal Price { get; set; }

        public decimal Amount { get; set; }

        public string? Note { get; set; }

        // Audit Properties
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
    }
}
