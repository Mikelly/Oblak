using Oblak.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Models.Api;

public class SrbPersonDto
{
    public int Id { get; set; }    
    public int? ExternalId { get; set; }
    public int? ExternalId2 { get; set; }
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
<<<<<<< HEAD

    public void SetEntity(SrbPerson srb)
    {
        srb.ArrivalType = this.ArrivalType;
        srb.BirthCountryIso2 = this.BirthCountryIso2;
        srb.BirthCountryIso3 = this.BirthCountryIso3;
        srb.BirthDate = this.BirthDate.Value;
        srb.BirthPlaceName = this.BirthPlaceName;
        //srb.CheckedIn = this.CheckedIn.Value;
        //srb.CheckOut = this.CheckOut.Value;
        srb.CheckIn = this.CheckIn;
        srb.CheckOut = this.CheckOut;
        srb.DocumentIssueDate = this.DocumentIssueDate;
        srb.DocumentNumber = this.DocumentNumber;
        srb.DocumentType = this.DocumentType;
        srb.EntryDate = this.EntryDate;
        srb.EntryPlace = this.EntryPlace;
        srb.EntryPlaceCode = this.EntryPlaceCode;
        srb.ExternalId = this.ExternalId;
        srb.ExternalId2 = this.ExternalId2;
        srb.FirstName = this.FirstName;
        srb.GroupId = this.GroupId;
        srb.Gender = this.Gender;
        srb.Guid = this.Guid;
        srb.IsDomestic = this.IsDomestic;
        srb.IsForeignBorn = this.IsForeignBorn.Value;
        srb.IssuingAuthorithy = this.IssuingAuthorithy;
        srb.LastName = this.LastName;
        srb.NationalityIso2 = this.NationalityIso2;
        srb.NationalityIso3 = this.NationalityIso3;
        srb.Note = this.Note;
        srb.NumberOfServices = this.NumberOfServices;
        srb.LegalEntityId = this.LegalEntityId.Value;
        srb.PersonalNumber = this.PersonalNumber;
        srb.PlannedCheckOut = this.PlannedCheckOut;
        //srb.PropertyExternalId = this.PropertyExternalId;
        srb.PropertyId = this.PropertyId.Value;        
        srb.ReasonForStay = this.ReasonForStay;
        srb.ResidenceCountryIso2 = this.ResidenceCountryIso2;
        srb.ResidenceCountryIso3 = this.ResidenceCountryIso3;
        srb.ResidenceMunicipalityCode = this.ResidenceMunicipalityCode;
        srb.ResidenceMunicipalityName = this.ResidenceMunicipalityName;
        srb.ResidencePlaceCode = this.ResidencePlaceCode;
        srb.ResidencePlaceName = this.ResidencePlaceName;
        srb.ResidenceTaxDiscountReason = this.ResidenceTaxDiscountReason;
        srb.ServiceType = this.ServiceType;
        srb.Status = this.Status;
        srb.StayValidTo = this.StayValidTo;
        srb.VisaNumber = this.VisaNumber;
        srb.VisaIssuingPlace = this.VisaIssuingPlace;
        srb.VisaType = this.VisaType;
    }
=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
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
<<<<<<< HEAD


=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
}
