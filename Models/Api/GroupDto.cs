﻿namespace Oblak.Models.Api
{
    public class GroupDto
    {
        public int Id { get; set; }
        public int? LegalEntityId { get; set; }
        public int? VesselId { get; set; }
        public int? NauticalLegalEntityId { get; set; }
        public int PropertyId { get; set; }
        public int? UnitId { get; set; }
        public string? GUID { get; set; }        
        public DateTime? Date { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }        
        public string? Email { get; set; }        
        public string? Status { get; set; }
        public string? EntryPoint { get; set; }
        public DateTime? EntryPointDate { get; set; }
        public decimal ResTaxAmount { get; set; }
        public decimal ResTaxFee { get; set;}
        public bool ResTaxCalculated { get; set; }
        public bool ResTaxPaid { get; set; }
        public int ResTaxPaymentTypeId { get; set; }
    }

    public class GroupEnrichedDto : GroupDto
    {
        public string? PropertyName { get; set; }
        public string? VesselDesc { get; set; }
        public string? NauticalLegalEntity { get; set; }
        public string? Guests { get; set; }
        public int? NoOfGuests { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
