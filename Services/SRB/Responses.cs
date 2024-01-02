namespace Oblak.Services.SRB.Models;



public class Rootobject
{
    public int id { get; set; }
    public string name { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public object title { get; set; }
    public object department { get; set; }
    public object displayName { get; set; }
    public string token { get; set; }
    public object rolesToken { get; set; }
    public string refreshToken { get; set; }
    public object telephoneNumber { get; set; }
    public bool jeBrisan { get; set; }
    public bool imaPravaAdministracije { get; set; }
    public bool imaPravaEvidentiranjaTuristeRezervacijeSmestaja { get; set; }
    public bool imaPravaEvidentiranjaBoravisneTakse { get; set; }
    public bool imaPravaZahtevZaKategorizaciju { get; set; }
    public bool imaPravaPretragePregledaObjekta { get; set; }
    public bool imaPravaPretragePregledaZahtevZaKategorizaciju { get; set; }
    public bool imaPravaZaIzvestaj { get; set; }
    public bool imaPravaPregledaVaucera { get; set; }
}



public class LoginResponse
{
    public int? id { get; set; }
    public string? name { get; set; }
    public string? firstName { get; set; }
    public string? lastName { get; set; }
    public string? username { get; set; }
    public string? email { get; set; }
    public object? title { get; set; }
    public object? department { get; set; }
    public object? displayName { get; set; }
    public string? token { get; set; }
    public object? rolesToken { get; set; }
    public string? refreshToken { get; set; }
    public object? telephoneNumber { get; set; }
    public bool? jeBrisan { get; set; }
    public bool? imaPravaAdministracije { get; set; }
    public bool? imaPravaEvidentiranjaTuristeRezervacijeSmestaja { get; set; }
    public bool? imaPravaEvidentiranjaBoravisneTakse { get; set; }
    public bool? imaPravaZahtevZaKategorizaciju { get; set; }
    public bool? imaPravaPretragePregledaObjekta { get; set; }
    public bool? imaPravaPretragePregledaZahtevZaKategorizaciju { get; set; }
    public bool? imaPravaZaIzvestaj { get; set; }
    public bool? imaPravaPregledaVaucera { get; set; }
}


public class LoginResponseOld
{
    public int id { get; set; }
    public string name { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public object title { get; set; }
    public object department { get; set; }
    public object displayName { get; set; }
    public string token { get; set; }
    public string refreshToken { get; set; }
    public Role[] roles { get; set; }
}

public class Role
{
    public int roleId { get; set; }
    public string roleName { get; set; }
    public int companyId { get; set; }
    public string companyCode { get; set; }
    public string companyName { get; set; }
    public object mb { get; set; }
    public object pib { get; set; }
    public object additionalField1 { get; set; }
    public object additionalField2 { get; set; }
    public object additionalField3 { get; set; }
    public object additionalField4 { get; set; }
    public object additionalField5 { get; set; }
    public Operation[] operations { get; set; }
}

public class Operation
{
    public int id { get; set; }
    public string name { get; set; }
}



public class CheckInOutResponse
{
    public string message { get; set; }
    public string identifikator { get; set; }
    public object[] warnings { get; set; }
    public SmestajnaJedinica[] smestajneJedinice { get; set; }
    public string[] errors { get; set; }
}


public class SmestajnaJedinica
{
    public string BrojSmestajneJedinice { get; set; }
    public string SpratSmestajneJedinice { get; set; }
    public int JedinstveniIdentifikator { get; set; }
    public int JeObrisan { get; set; }
    public DateTime? DatumBoravkaOd { get; set; }
    public DateTime? DatumBoravkaDo { get; set; }
}



public class ObjektiResponse
{
    public Objekti[] objekti { get; set; }
    public int brojObjekata { get; set; }
    public object objektiNaziv { get; set; }
}

public class Objekti
{
    public int id { get; set; }
    public string idObjekta { get; set; }
    public string nazivObjekta { get; set; }
    public int vrstaObjekta { get; set; }
    public string vrstaObjektaNaziv { get; set; }
    public string kategorijaObjekta { get; set; }
    public string identifikatorVlasnika { get; set; }
    public string vlasnik { get; set; }
    public string odgovornoLice { get; set; }
    public string adresa { get; set; }
    public string sifraOpstine { get; set; }
    public int tipLica { get; set; }
    public int? tipPravnogLica { get; set; }
    public bool zahtevanPrekidResenja { get; set; }
    public string sifraStatusa { get; set; }
    public bool privremeniPrekidRada { get; set; }
    public int ukupanBrojRapolozivihIndLez { get; set; }
    public DateTime datumKreiranja { get; set; }
    public bool pripadaSemi { get; set; }
    public object pripadaSemiTest { get; set; }
    public bool prijavaZakljucana { get; set; }
    public int kategorizacijaId { get; set; }
    public bool kategoriseSe { get; set; }
    public object statusZahteva { get; set; }
    public string brojResenja { get; set; }
    public bool uProcesuKategorizacije { get; set; }
    public object datumDonosenjaResenjaKat { get; set; }
    public object nazivOrganaDonosioca { get; set; }
}


public class TokenResponse
{
    public string UserId { get; set; }
    public string FullName { get; set; }
    public string nameid { get; set; }
    public string DodatnoPolje1 { get; set; }
    public string DodatnoPolje2 { get; set; }
    public string Email { get; set; }
    public string unique_name { get; set; }

}


public class TuristResponse
{
    public int totalRowsCount { get; set; }
    public TurisOneResponse[] data { get; set; }
}

public class TurisOneResponse
{
    public int turistaId { get; set; }    
}
