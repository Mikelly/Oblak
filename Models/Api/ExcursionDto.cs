using System;

namespace Oblak.Models.Api
{
    public class ExcursionDto
    {
        public int Id { get; set; }

        public int AgencyId { get; set; }

		public string AgencyName { get; set; }

		public DateTime Date { get; set; }

        public string VoucherNo { get; set; }

        public int? CountryId { get; set; }

        public string CountryName { get; set; }

        public string Status { get; set; }

        public int NoOfPersons { get; set; }

        public string? ExcursionTaxExempt { get; set; }

        public decimal ExcursionTaxAmount { get; set; }

        public decimal ExcursionTaxTotal { get; set; }


        // Audit Properties
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
    }
}
