using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Models.Api;

public class SrbPersonDto
{
    public int Id { get; set; }    
    public int? ExternalId { get; set; }
    public string? Guid { get; set; }
    public int? LegalEntityId { get; set; }
    public string? LegalEntityName { get; set; }
    public int? PropertyId { get; set; }
    public int? GroupId { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? PersonalNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }      
    public bool IsDomestic { get; set; }
    public bool? IsForeignBorn { get; set; }
    public string? BirthPlaceName { get; set; }
    public string? BirthCountryIso2 { get; set; }
    public string? BirthCountryIso3 { get; set; }
    public string? NationalityIso2 { get; set; }
    public string? NationalityIso3 { get; set; }
    public string? ResidenceCountryIso2 { get; set; }
    public string? ResidenceCountryIso3 { get; set; }
    public string? ResidenceMunicipalityCode { get; set; }
    public string? ResidenceMunicipalityName { get; set; }
    public string? ResidencePlaceCode { get; set; }
    public string? ResidencePlaceName { get; set; }

    #region IDENTIFIKACIONI DOKUMENT
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentIssueDate { get; set; }
    public string? VisaType { get; set; }
    // Samo prijava, samo strani gost, samo ako postoji vrsta vize
    public string? VisaNumber { get; set; }
    // Samo prijava, samo strani gost, samo ako postoji vrsta vize
    public string? VisaIssuingPlace { get; set; }
    public DateTime? EntryDate { get; set; }
    public string? EntryPlace { get; set; }
    public string? EntryPlaceCode { get; set; }
    public DateTime? StayValidTo { get; set; }
    public string? IssuingAuthorithy { get; set; }
    public string? Note { get; set; }
    #endregion

    #region BORAVAK
    public int? PropertyExternalId { get; set; }
    public string? ServiceType { get; set; }
    public string? ArrivalType { get; set; }
    public string? Vouchers { get; set; }

    #region SMJESTAJNE JEDINICE - Zaboravićemo ovo, hoteli nam nisu ciljna grupa
    /*
    public string? PropertyUnits { get; set; }

    public bool? IsDeleted { get; set; }

    public string UnitCheckIn { get; set; }

    public string UnitCheckOut { get; set; }
    */
    #endregion

    public DateTime? CheckIn { get; set; }
    [NotMapped]
    public bool? CheckedIn { get; set; }
    public DateTime? PlannedCheckOut { get; set; }
    public string? ResidenceTaxDiscountReason { get; set; }
    public string? ReasonForStay { get; set; }
    #endregion

    #region ODJAVA
    // Obavezno za pravna lica, nije za fizička
    // Samo za odjavu, domaći i strani gost
    public int? NumberOfServices { get; set; }
    // Format yyyy-MM-dd HH:mm
    // Datum ne može biti u budućnosti
    // Ne smije proći više od dozvoljenog roka za odjavu (koliki je taj rok?)
    // Samo za odjavu, domaći i strani gost
    public DateTime? CheckOut { get; set; }
    [NotMapped]
    public bool? CheckedOut { get; set; }
    #endregion
}

public class SrbPersonEnrichedDto : SrbPersonDto
{
    public string? FullName { get; set; } //
    public string? PropertyName { get; set; } //
    public bool? Registered { get; set; } = true;
    public bool? Locked { get; set; } = true;
    public bool? Deleted { get; set; } = false;
    public string? NationalityExternalId { get; set; }
    public string? BirthCountryExternalId { get; set; }
    public string? ResidenceCountryExternalId { get; set; }
}
