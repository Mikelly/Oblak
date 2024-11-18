using System;

namespace Oblak.Models.Api
{
    public class NauticalTaxDto
    {
        public int Id { get; set; }

        public int PartnerId { get; set; }        

        public string VesselType { get; set; }

        public decimal Amount { get; set; }

        public decimal LowerLimitLength { get; set; }

        public decimal UpperLimitLength { get; set; }

        public int LowerLimitPeriod { get; set; }

        public int UpperLimitPeriod { get; set; }

        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }

        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }
    }
}
