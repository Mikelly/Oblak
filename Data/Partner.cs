﻿using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Partner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(450)]
        public CountryEnum Country { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string? TIN { get; set; }

        [StringLength(450)]
        public PartnerType? PartnerType { get; set; }

        [StringLength(450)]
        public string? ResidenceTaxAccount { get; set; }

        [StringLength(450)]
        public string? ResidenceTaxName { get; set; }

        [StringLength(450)]
        public string? ResidenceTaxDescription { get; set; }

        public int? ResidenceTaxDaysLate { get; set; }

        public byte[]? Logo { get; set; }

        [StringLength(2000)]
        public string? Address { get; set; }

        [StringLength(2000)]
        public string? Reference { get; set; }

        public bool ResidenceTaxPaymentRequired { get; set; } = false;

		public bool UseAdvancePayment { get; set; } = false;

        public bool CheckRegistered { get; set; } = false;

        public bool SplitTaxAndFee { get; set; } = false;

        public int? NauticalTaxProperty { get; set; }

        public int DefaultPaymentId { get; set; } = 1;

        [StringLength(450)]
        public string Culture { get; set; }

        public bool UseEntryPointInGroup { get; set; } = false;

        public List<LegalEntity> LegalEntities { get; set; }
        public List<Computer> Computers { get; set; }

        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion

    }
}