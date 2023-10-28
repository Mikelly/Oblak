namespace Oblak.Models.Srb;

using global::Oblak.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SrbPersonViewModel
{
    // Prijava i odjava, domaće i strano lice, obavezan unos
    public bool Change { get; set; }

    // Prijava i odjava, domaće i strano lice, obavezan unos
    public bool IsDomestic { get; set; }

    // Prijava i odjava, domaće i strano lice, nije obavezan unos
    public bool IsForeignBorn { get; set; }

    // Samo prijava, samo strani gost
    public string? BirthPlaceName { get; set; }

    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje ISO 3
    public string? BirthCountryIso2 { get; set; }

    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje ISO 2
    public string? BirthCountryIso3 { get; set; }

    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje Nationality ISO 3
    public string? NationalityIso2 { get; set; }

    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje Nationality ISO 2
    public string? NationalityIso3 { get; set; }

    // Samo prijava, samo domaći gost, ne šalje se ako ima polje ISO 3
    public string? ResidenceCountryIso2 { get; set; }

    // Samo prijava, samo domaći gost, ne šalje se ako ima polje ISO 3
    public string? ResidenceCountryIso3 { get; set; }


    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    public string? ResidenceMunicipalityCode { get; set; }

    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    public string? ResidenceMunicipalityName { get; set; }

    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    public string? ResidencePlaceCode { get; set; }

    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    public string? ResidencePlaceName { get; set; }


    #region IDENTIFIKACIONI DOKUMENT


    // Samo prijava, samo strani gost, bira se iz šifarnika
    public string? DocumentType { get; set; }

    // Samo prijava, samo strani gost, bira se iz šifarnika
    public string? DocumentNumber { get; set; }

    // Samo prijava, samo strani gost, samo ako postoji dokument
    // Format yyyy-MM-dd HH:mm
    public DateTime DocumentIssueDate { get; set; }

    // Samo prijava, samo strani gost, samo ako postoji dokument, bira se iz šifarnika        
    public string? VisaType { get; set; }

    // Samo prijava, samo strani gost, samo ako postoji vrsta vize
    public string? VisaNumber { get; set; }

    // Samo prijava, samo strani gost, samo ako postoji vrsta vize
    public string? VisaIssuingPlace { get; set; }


    #region SUMNJIVA LICA - Nije nam predmet interesovanja


    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 5
    public DateTime EntryDate { get; set; }

    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 5
    public string? EntryPlaceCode { get; set; }

    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 5
    public DateTime StayValidTo { get; set; }

    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 3
    public string? IssuingAuthorithy { get; set; }


    #endregion


    // Samo prijava, samo strani gost, nije obavezno
    public string? Note { get; set; }


    #endregion


    #region BORAVAK


    // Prijava i odjava, domaći i strani gost, obavezan unos
    public int PropertyRefId { get; set; }

    // Samo prijava, domaći i strani gost, obavezan unos, iz šifarnika
    public string ServiceType { get; set; }

    // Samo prijava, domaći i strani gost, obavezan unos, iz šifarnika
    public string ArrivalType { get; set; }

    // Samo prijava, samo domaći gost, obavezan unos ako je način dolaska "5"
    public string? Vouchers { get; set; }



    #region SMJESTAJNE JEDINICE - Zaboravićemo ovo, hoteli nam nisu ciljna grupa

    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća" ili "apartman"
    // Unutar smještajne jedinice ima broj, sprat, je obrisan i 
    public string? PropertyUnits { get; set; }

    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća", "apartman" ili "soba"
    // Ne može biti 1 na inicijalnoj prijavi
    // Dio smještajne jedinice
    public bool? IsDeleted { get; set; }

    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća", "apartman" ili "soba"
    // Na inicijalnoj prijavi može biti prazan
    // Dio smještajne jedinice
    public string UnitCheckIn { get; set; }

    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća", "apartman" ili "soba"
    // Na inicijalnoj prijavi može biti prazan
    // Dio smještajne jedinice
    public string UnitCheckOut { get; set; }

    #endregion



    // Samo prijava, samo domaći gost, obavezan unos
    // Format yyyy-MM-dd HH:mm
    public DateTime CheckIn { get; set; }

    // Samo prijava, domaći i strani gost, obavezan unos
    // Format yyyy-MM-dd HH:mm
    public DateTime PlannedCheckOut { get; set; }

    // Samo prijava, domaći i strani gost, nije obavezan unos, bira se iz šifarnika
    // Ne važi za fizička lica
    public string? ResidenceTaxDiscountReason { get; set; }

    // Samo prijava, domaći i strani gost, obavezan unos, bira se iz šifarnika        
    public string ReasonForStay { get; set; }

    #endregion


    #region ODJAVA

    // Obavezno za pravna lica, nije za fizička
    // Samo za odjavu, domaći i strani gost
    public int? NumberOfServices { get; set; }

    // Format yyyy-MM-dd HH:mm
    // Datum ne može biti u budućnosti
    // Ne smije proći više od dozvoljenog roka za odjavu (koliki je taj rok?)
    // Samo za odjavu, domaći i strani gost
    public DateTime CheckOut { get; set; }

    #endregion

    public int Id { get; set; }
    public int? RefId { get; set; }
    public int LegalEntityId { get; set; }
    public int PropertyId { get; set; }
    public int GroupId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string PersonalNumber { get; set; }
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string Status { get; set; }
    public string Error { get; set; }
}
