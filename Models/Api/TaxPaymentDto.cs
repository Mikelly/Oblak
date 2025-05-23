﻿using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
	public class TaxPaymentDto
	{
		public int Id { get; set; }
		public int? LegalEntityId { get; set; }
        public int? AgencyId { get; set; }
        public int? PersonId { get; set; }
        public int? GroupId { get; set; }
        public int? InvoiceId { get; set; }
        public int TaxPaymentTypeId { get; set; }
        public TaxType TaxType { get; set; }
		public DateTime? TransactionDate { get; set; }
		public decimal? Amount { get; set; }
        public decimal? Fee { get; set; }
        public string? Note { get; set; }
        public string? Reference { get; set; }
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
    }
}
