using Oblak.Data;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public int? ExternalId { get; set; }
        public int LegalEntityId { get; set; }
        public string? LegalEntityName { get; set; }
        public string Name { get; set; }
        public string? Type { get; set; }
        public string? Address { get; set; }
        public int? MunicipalityId { get; set; }
        public string? Place { get; set; }
        public decimal? Price { get; set; }
        public string? BusinessUnitCode { get; set; }
        public string? FiscalEnuCode { get; set; }
        public string? Status { get; set; }
        public string? DefaultEntryPoint { get; set; }
        public bool AutoCheckOut { get; set; }
        public TimeSpan? AutoCheckOutTime { get; set; }
        public decimal? ResidenceTax { get; set; }
        public bool? ResidenceTaxYN { get; set; }
        public int? Capacity { get; set; }
        public string? RegNumber { get; set; }
        public DateTime? RegDate { get; set; }

        public Property ToEntity(Property property)
        {
            object m;            
            property.ExternalId = this.ExternalId ?? 0;
            property.LegalEntityId = this.LegalEntityId;
            property.Name = this.Name;
            property.Type = this.Type;
            property.Address = this.Address;
            property.MunicipalityId = this.MunicipalityId;
            property.Place = this.Place;
            property.Price = this.Price;
            property.BusinessUnitCode = this.BusinessUnitCode;
            property.FiscalEnuCode = this.FiscalEnuCode;
            property.Status = this.Status ?? "A";
            property.DefaultEntryPoint = this.DefaultEntryPoint;
            property.AutoCheckOut = this.AutoCheckOut;
            property.AutoCheckOutTime = this.AutoCheckOutTime;
            property.ResidenceTax = this.ResidenceTax;
            property.ResidenceTaxYN = this.ResidenceTaxYN;
            property.Capacity = this.Capacity;
            property.RegNumber = this.RegNumber;
            property.RegDate = this.RegDate;

            return property;
        }
    }

    public class PropertyEnrichedDto : PropertyDto
    {
        public string? PaymentType { get; set; }
        public string? PropertyName { get; set; }
        public string? LegalEntity { get; set; }
        public string? TIN { get; set; }
    }
}
