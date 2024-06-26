using Kendo.Mvc.UI.Fluent;
using Oblak.Data;

namespace Oblak.Helpers;

public static class Kendo
{
    public static void FilterConfig(GridBoundColumnFilterableBuilder f)
    {
        f.Enabled(true)
            .Messages(m => m.IsFalse("nije").IsTrue("jeste")).Search(true)
            .Operators(o =>
            {
                o.ForString(os => os.Clear().Contains("sadrži").DoesNotContain("ne sadrži").StartsWith("počinje sa").EndsWith("se završava na").IsEqualTo("je jednak").IsNotEqualTo("nije jednak").IsNullOrEmpty("je prazan").IsNotNullOrEmpty("nije prazan"));
                o.ForNumber(os => os.Clear().IsEqualTo("je jednak").IsNotEqualTo("nije jednak").IsGreaterThan("je veći od").IsGreaterThanOrEqualTo("je veći od ili je jedank").IsLessThan("je manji od").IsLessThanOrEqualTo("je manji od ili je jednak").IsNull("je prazan").IsNotNull("nije prazan"));
                o.ForDate(os => os.Clear().IsEqualTo("je jednak").IsNotEqualTo("nije jednak").IsGreaterThan("je nakon").IsGreaterThanOrEqualTo("je nakon ili je jedank").IsLessThan("je prije").IsLessThanOrEqualTo("je prije ili je jednak").IsNull("je prazan").IsNotNull("nije prazan"));
                o.ForEnums(os => os.Clear().IsEqualTo("je jednak").IsNotEqualTo("nije jednak").IsNull("je prazan").IsNotNull("nije prazan"));
            });
    }
}

public class Exceptions
{
    public static string StringException(Exception e)
    {
        string retval = string.Empty;

        while (e != null)
        {
            retval += e.Message + Environment.NewLine;
            e = e.InnerException;
        }

        return retval;
    }

    public static string ParseException(Exception excp)
    {
        if (excp is System.ServiceModel.FaultException)
        {
            var fault = (excp as System.ServiceModel.FaultException).CreateMessageFault();
            if (fault.HasDetail)
            {
                var faultDetails = fault.GetDetail<System.Xml.XmlElement>();
                var code = faultDetails.InnerText;
                if (code == "11")
                {
                    if (excp.Message.ToLower().StartsWith("tcr is not valid")) return "ENU uređaj je tek registrovan u Poreskoj upravi, i još nije aktivan. ENU se aktivira 60 min od momentra registracije. Pokušajte malo kasnije!";
                    if (excp.Message.ToLower().StartsWith("total vat amount is wrong")) return "Ukupan iznos PDV-a nije ispravan!";
                    if (excp.Message.ToLower().StartsWith("type of invoice doesn't match payment method")) return "Na ovom računu ne možete koristiti izabrani način plaćanja!";
                    if (excp.Message.ToLower().Contains("the content of element 'items' is not complete")) return "Ne možete fiskalizovati dokument koji nema stavke!";
                    if (excp.Message.ToLower().Contains("buyers tin is not in the correct format")) return "Neispravno unesen PIB kupca";
                    if (excp.Message.ToLower().Contains("status of the taxpayer was not active at the time when the invoice was issued")) return "Poreski obveznik nije bio aktivan u momentu izdavanja računa";
                    if (excp.Message.ToLower().Contains("software code does not match tcr software code")) return "Neispravan kôd softvera za ENU";
                    if (excp.Message.ToLower().Contains("operator code is not valid")) return "Neispravan kôd operatera";
                    if (excp.Message.ToLower().Contains("code=920")) return "Greška u fiskalnom poreskom servisu. Nemojte praviti novi dokument, već fiskalizujte ovaj kada se poreski servis ponovo uspostavi";
                    if (excp.Message.ToLower().Contains("business unit code is not valid")) return "Neispravan kôd poslovne jedinice";
                    if (excp.Message.ToLower().Contains("tax rate is not valid in the moment of invoice issuing")) return "Nevažeća poreska stopa u momentu izdavanja računa";
                    if (excp.Message.ToLower().Contains("total invoice price is wrong")) return "Ukupan iznos računa je neispravan";
                    if (excp.Message.ToLower().Contains("total price amount doesn't coresponds to amount in the paymentmethods")) return "Ukupan iznos računa se ne poklapa sa iznosima u načinima plaćanja";
                    if (excp.Message.ToLower().Contains("invoice vat is invalid.totvatamt attribute must not exist if taxpayer is not obligated to declare vat")) return "Neispravan PDV na računu. Ukoliko niste obveznik PDV-a, ne treba da prikazujete ukupan PDV na računu";
                    if (excp.Message.ToLower().Contains("invoice vat is invalid. sametaxes element must not exist if taxpayer is not obligated to declare vat")) return "Neispravan PDV na računu. Ukoliko niste obveznik PDV-a, ne treba da prikazujete PDV na računu";
                    if (excp.Message.ToLower().Contains("issuedatetime must be equal to senddatetime if subseqdelivtype does not exist")) return "Vrijeme izdavanja računa mora biti isto kao vrijeme fiskalizacije ako ne navedete razlog naknadne fiskalizacije";
                    if (excp.Message.ToLower().Contains("changedatetime must be equal to senddatetime if subseqdelivtype does not exist")) return "Vrijeme izdavanja računa mora biti isto kao vrijeme fiskalizacije ako ne navedete razlog naknadne fiskalizacije";
                    if (excp.Message.ToLower().Contains("negative values are allowed only when correctiveInv exist")) return "Negativne količine su dozvoljene samo na korektivnom računu";
                    if (excp.Message.ToLower().Contains("issuedatetime must not be older than 7 days if subseqdelivtype is equal to boundbook or businesneeds")) return "Datum izdavanja računa ne smije biti više od 7 dana u prošlosti ukoliko razlog naknadne fiskalizacije nije BOUNDBOOK (problem na naplatnom uređaju) ili BUSINESSNEED (poslovna potreba)";
                    if (excp.Message.ToLower().Contains("issuedatetime must not be older than 48h days if subseqdelivtype equals to service or technicalerror")) return "Datum izdavanja računa ne smije biti više od 48 sati u prošlosti ukoliko razlog naknadne fiskalizacije nije SERVICE (problem u fiskalnom servisu) ili TECHICAL ERROR (tehnička greška)";
                    if (excp.Message.ToLower().Contains("attribute 'u' must appear on element 'i'")) return "Nijeste unijeli jedinicu mjere na nekoj stavci računa";
                    if (excp.Message.ToLower().Contains("attribute 'businunitcode' must appear on element 'tcr'")) return "Nepostojeći kôd poslovne jedinice";
                    if (excp.Message.ToLower().Contains("tax rate or exempt from vat must exist for all same taxes if issuer is in vat")) return "Ukoliko je izdavaoc fakture obveznik PDV-a, sumarni porezi moraju prikazati stopu poreza ili oslobođenje poreza. ";
                    if (excp.Message.ToLower().Contains("the content of element 'iicrefs' is not complete")) return "Morate odabrati barem jedan vezani dokument, u slučaju kada fiskalizujete knjižno odobrenje, avansni račun ili periodičnu fakturu.";

                    return excp.Message;
                }


                if (code == "34") { return "Dostavljeni certifikat nije valjan"; }
                if (code == "35") { return "Certifikat nije izdao Registrovani CA"; }
                if (code == "36") { return "Certifikat je istekao"; }
                if (code == "37") { return "Uporediti PIB u XML - u s PIB-om u certifikatu"; }
                if (code == "38") { return "Certifikat je opozvan"; }
                if (code == "39") { return "Status certifikata je nepoznat"; }
                if (code == "40") { return "Iznos računa prevelik za račun u gotovini"; }
                if (code == "41") { return "Kôd poslovne jedinice ne odnosi se na aktivnu poslovnu jedinicu(prostor) poreskog obveznika"; }
                if (code == "42") { return "Kôd softvera ne odnosi se na aktivni softver"; }
                if (code == "43") { return "Kôd održavaoca ne odnosi se na aktivnog održavaoca"; }
                if (code == "44") { return "Status PDV - a izdavaoca ne odgovara atributu \"izdavalac je obveznik PDV-a\""; }
                if (code == "45") { return "'Važi od' ne može biti u prošlosti"; }
                if (code == "46") { return "'Važi do' ne može biti u prošlosti"; }
                if (code == "47") { return "'Važi do' ne može biti prije 'važi od'"; }
                //if (code == "48") { return "Aktivni ENU nije moguće ažurirati"; }
                if (code == "48") { return "Kod aktivnog ENU-a možete mijenjati samo podatke 'validTo' (do kada je aktivan) i 'maintainerCode' (šifra održavaoca)!"; }
                if (code == "49") { return "Promjene u datumu i vremenu razlikuju se u minutima od vremena iz Centralnog registra računa više nego što je to dozvoljeno (6 sati)"; }
                if (code == "50") { return "Iznos gotovine za INITIAL operaciju ne može biti negativan"; }
                if (code == "51") { return "Iznos gotovine ne može biti nula za operaciju WITHDRAW"; }
                if (code == "52") { return "Poreski obveznik ne postoji u Registru poreskih obveznika"; }
                if (code == "53") { return "ENU kôd klijenta ne odnosi se na registrovani ili aktivni ENU ili ENU ne pripada navedenom izdavaču"; }
                if (code == "54") { return "Vrsta identifikacije mora biti TIN (JMB/ PIB)"; }
                if (code == "55") { return "Poreski obveznik nije aktivan u registru poreskih obveznika"; }
                if (code == "56") { return "Ne možete izvršiti unos depozita jer postoje fiskalizovani računi za tekući radni dan!"; }
                if (code == "57") { return "Deaktivirani ENU se ne može mijenjati!"; }
                if (code == "58") { return "Ne možete izvršiti fiskalizaciju računa prije nego što unesete inicijalni depozit za tekući dan!"; }
            }
        }

        return Exceptions.StringException(excp);
    }
}
