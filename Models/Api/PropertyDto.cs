namespace Oblak.Models.Api
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public int? ExternalId { get; set; }
        public int LegalEntityId { get; set; }
        public string LegalEntityName { get; set; }
        public string Name { get; set; }
        public string? Type { get; set; }
        public string? Address { get; set; }
        public string? Municipality { get; set; }
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
    }

    public class PropertyEnrichedDto : PropertyDto
    {
        public string? PaymentType { get; set; }
        public string? PropertyName { get; set; }
        public string? LegalEntity { get; set; }
    }
}
