using System.ComponentModel.DataAnnotations;

namespace Oblak.Data;

public class SrbPerson : Person
{    
    [Obsolete("Da li je lice domaće")]
    // Prijava i odjava, domaće i strano lice, obavezan unos
    public bool IsDomestic { get; set; }

    [Obsolete("Da li je lice rođemo u inostranstvu")]
    // Prijava i odjava, domaće i strano lice, nije obavezan unos
    public bool IsForeignBorn { get; set; }

    [Obsolete("Mjesto rođenja")]
    [StringLength(450)]
    // Samo prijava, samo strani gost
    public string? BirthPlaceName { get; set; }

    [Obsolete("Država rođenja 2")]
    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje ISO 3
    [StringLength(2)]
    public string? BirthCountryIso2 { get; set; }

    [Obsolete("Država rođenja 3")]
    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje ISO 2
    [StringLength(3)]
    public string? BirthCountryIso3 { get; set; }

    [Obsolete("Nacionalnost 2")]
    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje Nationality ISO 3
    [StringLength(2)]
    public string? NationalityIso2 { get; set; }

    [Obsolete("Nacionalnost 3")]
    // Samo prijava, domaći i strani gost, ne šalje se ako ima polje Nationality ISO 2
    [StringLength(3)]
    public string? NationalityIso3 { get; set; }

    [Obsolete("Država prebivališta ISO 2")]
    // Samo prijava, samo domaći gost, ne šalje se ako ima polje ISO 3
    [StringLength(2)]
    public string? ResidenceCountryIso2 { get; set; }

    [Obsolete("Država prebivališta ISO 3")]
    // Samo prijava, samo domaći gost, ne šalje se ako ima polje ISO 3
    [StringLength(3)]
    public string? ResidenceCountryIso3 { get; set; }


    [Obsolete("Opština prebivališta matični broj")]
    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    [StringLength(450)]
    public string? ResidenceMunicipalityCode { get; set; }

    [Obsolete("Opština prebivališta naziv")]
    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    [StringLength(450)]
    public string? ResidenceMunicipalityName { get; set; }

    [Obsolete("Mjesto prebivališta matični broj")]
    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    [StringLength(450)]
    public string? ResidencePlaceCode { get; set; }

    [Obsolete("Mjesto prebivališta naziv")]
    // Samo prijava, samo domaći gost, i ako je država prebivališta Srbija
    [StringLength(450)]
    public string? ResidencePlaceName { get; set; }


    #region IDENTIFIKACIONI DOKUMENT


    [Obsolete("Vrsta putne isprave")]
    // Samo prijava, samo strani gost, bira se iz šifarnika
    [StringLength(450)]
    public string? DocumentType { get; set; }

    [Obsolete("Broj putne isprave")]
    // Samo prijava, samo strani gost, bira se iz šifarnika
    [StringLength(450)]
    public string? DocumentNumber { get; set; }

    [Obsolete("Datum izdavanja putne isprave")]
    // Samo prijava, samo strani gost, samo ako postoji dokument
    // Format yyyy-MM-dd HH:mm
    public DateTime? DocumentIssueDate { get; set; }

    [Obsolete("Vrsta vize šifra")]
    // Samo prijava, samo strani gost, samo ako postoji dokument, bira se iz šifarnika        
    [StringLength(450)]
    public string? VisaType { get; set; }

    [Obsolete("Viza broj")]
    // Samo prijava, samo strani gost, samo ako postoji vrsta vize
    [StringLength(450)]
    public string? VisaNumber { get; set; }

    [Obsolete("Viza mjesto izdavanja")]
    // Samo prijava, samo strani gost, samo ako postoji vrsta vize
    [StringLength(450)]
    public string? VisaIssuingPlace { get; set; }

    
    #region SUMNJIVA LICA - Nije nam predmet interesovanja


    [Obsolete("Datum ulaska u rebupliku srbiju")]
    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 5
    public DateTime? EntryDate { get; set; }

    [Obsolete("Mjesto ulaska u rebupliku srbiju - Šifra")]
    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 5
    [StringLength(450)]
    public string? EntryPlaceCode { get; set; }

    [Obsolete("Mjesto ulaska u rebupliku srbiju - Naziv")]
    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 5
    [StringLength(450)]
    public string? EntryPlace { get; set; }

    [Obsolete("Datum do kada je odobren borakav u republici srbiji")]
    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 5    
    public DateTime? StayValidTo { get; set; }

    [Obsolete("Organ izdavanja putne isprave")]
    // Samo prijava, samo strani gost, samo ako je tip dokumenta 1 - 3
    [StringLength(450)]
    public string? IssuingAuthorithy { get; set; }



    #endregion


    [Obsolete("Napomena")]
    // Samo prijava, samo strani gost, nije obavezno
    [StringLength(4000)]
    public string? Note { get; set; }


    #endregion


    #region BORAVAK


    [Obsolete("Ugostiteljski objekat Identifikator")]        
    // Prijava i odjava, domaći i strani gost, obavezan unos
    public int PropertyExternalId { get; set; }

    [Obsolete("Vrsta pruženih usluga")]
    // Samo prijava, domaći i strani gost, obavezan unos, iz šifarnika
    [StringLength(450)]
    public string ServiceType { get; set; }

    [Obsolete("Način dolaska")]
    // Samo prijava, domaći i strani gost, obavezan unos, iz šifarnika
    [StringLength(450)]
    public string ArrivalType { get; set; }

    [Obsolete("Barkodovi vaučera")]
    // Samo prijava, samo domaći gost, obavezan unos ako je način dolaska "5"
    [StringLength(4000)]
    public string? Vouchers { get; set; }


    /*
    #region SMJESTAJNE JEDINICE - Zaboravićemo ovo, hoteli nam nisu ciljna grupa

    [Obsolete("Smještajne jedinice")]
    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća" ili "apartman"
    // Unutar smještajne jedinice ima broj, sprat, je obrisan i 
    public string? PropertyUnits { get; set; }

    [Obsolete("Je obrisan")]
    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća", "apartman" ili "soba"
    // Ne može biti 1 na inicijalnoj prijavi
    // Dio smještajne jedinice
    public bool? IsDeleted { get; set; }

    [Obsolete("Datum boravka od")]
    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća", "apartman" ili "soba"
    // Na inicijalnoj prijavi može biti prazan
    // Dio smještajne jedinice
    public string? UnitCheckIn { get; set; }

    [Obsolete("Datum boravka do")]
    // Samo prijava, domaći i strani gost, obavezan unos osim ako je vrsta objekta "kuća", "apartman" ili "soba"
    // Na inicijalnoj prijavi može biti prazan
    // Dio smještajne jedinice
    public string? UnitCheckOut { get; set; }

    #endregion
    */


    [Obsolete("Datum i čas dolaska")]
    // Samo prijava, samo domaći gost, obavezan unos
    // Format yyyy-MM-dd HH:mm
    public DateTime? CheckIn { get; set; }

    public bool CheckedIn { get; set; }

    [Obsolete("Planirani datum i čas odlaska")]
    // Samo prijava, domaći i strani gost, obavezan unos
    // Format yyyy-MM-dd HH:mm
    public DateTime? PlannedCheckOut { get; set; }

    [Obsolete("Uslov za umanjenje boravišne takse")]
    // Samo prijava, domaći i strani gost, nije obavezan unos, bira se iz šifarnika
    // Ne važi za fizička lica
    [StringLength(450)]
    public string? ResidenceTaxDiscountReason { get; set; }

    [Obsolete("Razlog boravka")]
    // Samo prijava, domaći i strani gost, obavezan unos, bira se iz šifarnika        
    [StringLength(450)]
    public string ReasonForStay { get; set; }

    #endregion


    #region ODJAVA

    [Obsolete("Broj pruženih usluga")]
    // Obavezno za pravna lica, nije za fizička
    // Samo za odjavu, domaći i strani gost
    public int? NumberOfServices { get; set; }

    [Obsolete("Datum i čas odjave")]
    // Format yyyy-MM-dd HH:mm
    // Datum ne može biti u budućnosti
    // Ne smije proći više od dozvoljenog roka za odjavu (koliki je taj rok?)
    // Samo za odjavu, domaći i strani gost
    public DateTime? CheckOut { get; set; }

    public bool CheckedOut { get; set; }

    #endregion
}