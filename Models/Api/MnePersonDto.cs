namespace Oblak.Models.Api
{
    public class MnePersonDto
    {
        public int Id { get; set; }
        public int? ExternalId { get; set; }
        public string? Guid { get; set; }
        public int LegalEntityId { get; set; }
        public int PropertyExternalId { get; set; }
        public int PropertyId { get; set; }
        public int GroupId { get; set; }
        public int? UnitId { get; set; }
        public string LastName { get; set; } // 
        public string FirstName { get; set; } //
        public string? PersonalNumber { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsDeleted { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
        public string BirthPlace { get; set; }
        public string BirthCountry { get; set; }
        public string Nationality { get; set; }
        public string PersonType { get; set; } //
        public string PermanentResidenceCountry { get; set; }
        public string PermanentResidencePlace { get; set; }
        public string PermanentResidenceAddress { get; set; }
        public string? ResidencePlace { get; set; }
        public string? ResidenceAddress { get; set; }
        public DateTime CheckIn { get; set; } //
        public DateTime? CheckOut { get; set; } //
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentValidTo { get; set; }
        public string DocumentCountry { get; set; }
        public string DocumentIssuer { get; set; }
        public string? VisaType { get; set; } //
        public string? VisaNumber { get; set; } // 
        public DateTime? VisaValidFrom { get; set; }
        public DateTime? VisaValidTo { get; set; } //
        public string? VisaIssuePlace { get; set; }
        public string? EntryPoint { get; set; }
        public DateTime? EntryPointDate { get; set; }
        public string? Other { get; set; }
    }

    public class MnePersonEnrichedDto : MnePersonDto
    {
        public string? FullName { get; set; } //
        public string? PropertyName { get; set; } //
        public bool? Registered { get; set; } = true;
        public bool? Locked { get; set; } = true;
        public bool? Deleted { get; set; } = false;
    }
}