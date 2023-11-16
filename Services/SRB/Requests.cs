namespace Oblak.Services.SRB.Models;


public class LoginRequest
{
    public string korisnickoIme { get; set; }
    public string lozinka { get; set; }
}


public class CheckOutRequest
{
    public bool Izmena { get; set; }
    public DateTime DatumICasOdjave { get; set; }
    public int BrojPruzenihUslugaSmestaja { get; set; }
    public int UgostiteljskiObjekatJedinstveniIdentifikator { get; set; }
    public string ExternalId { get; set; }
}

public class CheckInRequest
{
    public osnovniPodaci OsnovniPodaci { get; set; }

    public identifikacioniDokumentStranogLica IdentifikacioniDokumentStranogLica { get; set; }

    public podaciOBoravku PodaciOBoravku { get; set; }
}

public class podaciOBoravku
{
    public string UgostiteljskiObjekatJedinstveniIdentifikator { get; set; }

    public string VrstaPruzenihUslugaSifra { get; set; }

    public string NacinDolaskaSifra { get; set; }

    public string[] BarkodoviVaucera { get; set; }

    public string NazivAgencije { get; set; }

    public SmestajnaJedinica[] SmestajneJedinice { get; set; }

    public string? DatumICasDolaska { get; set; }

    public string? PlaniraniDatumOdlaska { get; set; }

    public string UslovZaUmanjenjeBoravisneTakseSifra { get; set; }

    public string RazlogBoravkaSifra { get; set; }

    public bool ShouldSerializeSmestajneJedinice()
    {
        return false;
    }
}

public class identifikacioniDokumentStranogLica
{
    public string? VrstaPutneIspraveSifra { get; set; }

    public string? BrojPutneIsprave { get; set; }

    public string? DatumIzdavanjaPutneIsprave { get; set; }

    public string? VrstaVizeSifra { get; set; }

    public string? BrojVize { get; set; }

    public string? MestoIzdavanjaVize { get; set; }

    public string? DatumUlaskaURepublikuSrbiju { get; set; }

    public string? MestoUlaskaURepublikuSrbijuSifra { get; set; }

    public string? MestoUlaskaURepublikuSrbiju { get; set; }

    public DateTime? DatumDoKadaJeOdobrenBoravakURepubliciSrbiji { get; set; }

    public string Napomena { get; set; }

    public string OrganIzdavanjaPutneIsprave { get; set; }
}


public class osnovniPodaci
{
    public string ExternalId { get; set; }

    public bool Izmena { get; set; }

    public bool DaLiJeLiceDomace { get; set; }

    public bool DaLiJeLiceRodjenoUInostranstvu { get; set; }

    public string? Ime { get; set; }

    public string? Prezime { get; set; }

    public string? DatumRodjenja { get; set; }

    public string? PolSifra { get; set; }

    public string? Jmbg { get; set; }

    public string? MestoRodjenjaNaziv { get; set; }

    public string? DrzavaRodjenjaAlfa2 { get; set; }

    public string? DrzavaRodjenjaAlfa3 { get; set; }

    public string? DrzavljanstvoAlfa2 { get; set; }

    public string? DrzavljanstvoAlfa3 { get; set; }

    public string? OpstinaPrebivalistaMaticniBroj { get; set; }

    public string? OpstinaPrebivalistaNaziv { get; set; }

    public string? MestoPrebivalistaMaticniBroj { get; set; }

    public string? MestoPrebivalistaNaziv { get; set; }

    public string? DrzavaPrebivalistaAlfa2 { get; set; }

    public string? DrzavaPrebivalistaAlfa3 { get; set; }

    public bool ShouldSerializeJmbg()
    {
        if (this.DaLiJeLiceDomace) return true;
        else return false;
    }
}

public class ObjektiRequest
{
    public string ugostiteljId { get; set; }
    public int pageSize { get; set; }
    public int pageIndex { get; set; }
    public int ownerJmbg { get; set; }
}


public class TuristRequest
{
    public string ime { get; set; }
    public string prezime { get; set; }        
    public string datumIvremeDolaskaOd { get; set; }    
    public string datumIvremeDolaskaDo { get; set; }
    public string casDolaskaOd { get; set; }
    public string casDolaskaDo { get; set; }
    public string datumIvremeOdlaskaOd { get; set; }    
    public string datumIvremeOdlaskaDo { get; set; }
    public string casOdlaskaOd { get; set; }
    public string casOdlaskaDo { get; set; }
    public int[] ugostiteljskiObjekatIds { get; set; }
    public int pageIndex { get; set; }
    public int pageSize { get; set; }

}
