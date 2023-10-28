using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using RB90;
using System.Globalization;
using Oblak.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Oblak.Services;
using Microsoft.EntityFrameworkCore;
using Telerik.Windows.Documents.Flow.FormatProviders.Docx;
using Telerik.Windows.Documents.Flow.Model;
using Telerik.Windows.Documents.Flow.Model.Editing;
using Telerik.Windows.Documents.Flow.FormatProviders.Pdf;
using Oblak.Helpers;
using Oblak.Models.rb90;
using System.Runtime.ConstrainedExecution;
using Oblak.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Oblak.SignalR;
using Oblak.Models.Api;
using Oblak.Services.SRB.Models;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using AutoMapper;

namespace Oblak.Services.MNE
{
    public class MneClient : Register
    {
        readonly ILogger<MneClient> _logger;
        readonly mup _rb90client;
        IMapper _mapper;
        ApplicationUser _user;
        LegalEntity _legalEntity;
        X509Certificate2 _cert;
        user _rb90User;

        public MneClient(
            IConfiguration configuration,
            ILogger<MneClient> logger,
            IHttpContextAccessor contextAccesor,
            mup rb90client,
            ApplicationDbContext db,
            SelfRegisterService selfRegisterService,
            eMailService eMailService,
            IHubContext<MessageHub> messageHub,
            IWebHostEnvironment webHostEnvironment) : base(configuration, contextAccesor, eMailService, selfRegisterService, webHostEnvironment, messageHub, db)
        {
            _logger = logger;            
            _rb90client = rb90client;
            
            var username = _context.User.Identity!.Name;
            if (username != null)
            {
                _user = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username)!;
                if(_user != null) _legalEntity = _user.LegalEntity;
            }
        }

        public (user, smjestajniObjekat[], X509Certificate2) Auth(Property property)
        {   
			var cert = new X509Certificate2(property.LegalEntity.Rb90CertData, property.LegalEntity.Rb90Password);
			var issuer = cert.Issuer.Split(new char[] { ',' });
            var cn = issuer.Where(a => a.Trim().StartsWith("CN")).FirstOrDefault();
            cn = cn!.Split(new char[] { '=' })[1].Trim();
            var CertUser = cert.GetSerialNumberString() + "@" + cn;
            var authResponse = _rb90client.authenticate(new authenticateRequest(CertUser));         

            return (authResponse.user, authResponse.listaSmjestajnihObjekata, cert);
        }        

        public override async Task<List<PersonErrorDto>> RegisterGroup(Group group, DateTime? checkInDate, DateTime? checkOutDate)
        {
            try
            {
                var test = _user.Type == Data.Enums.UserType.Test;
                var data = _db.MnePersons.Where(a => a.GroupId == group.Id).ToList();

                if (checkInDate.HasValue)
                {
                    foreach (var pr in data) pr.CheckIn = checkInDate.Value;
                }

                if (checkOutDate.HasValue)
                {
                    foreach (var pr in data) pr.CheckOut = checkOutDate.Value;
                }

                foreach (var pr in data)
                {
                    if (pr.CheckOut == null) pr.CheckOut = DateTime.Now;
                }

                user user = null;
                smjestajniObjekat[] so = null;
                X509Certificate2 cert = null;

                var errors = Validate(group, checkInDate, checkOutDate);

                if (errors.Any()) return errors;

                if (test)
                {
                    foreach (var pr in data) pr.ExternalId = 0;                    
                    _db.SaveChanges();
                    return null;
                }

                int total = data.Count();

                try
                {
                    (user, so, cert) = ((user, smjestajniObjekat[], X509Certificate2)) await Authenticate();
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    _logger.LogDebug("SEND2MUP AUTH ERROR - RB: " + Exceptions.StringException(e));
                    throw new Exception("Greška prilikom autentifikacije na RB90 servis.");
                }

                int c = 0;
                foreach (var pr in data.OrderBy(a => a.ExternalId))
                {
                    c++;
                    await _messageHub.Clients.User(_context.User.Identity!.Name!).SendAsync("status",
                        (int)(Math.Round((decimal)c / (decimal)total * 100m, 0, MidpointRounding.AwayFromZero)),
                        $"Prijavljivanje gostiju {c}/{total}", $"{pr.FirstName} {pr.LastName}"
                        );

                    var error = sendOne2Mup(pr, user, cert, false);
                    if (error != null) pr.Error = error;
                    Thread.Sleep(100);
                }

                await _messageHub.Clients.User(_context.User.Identity!.Name!).SendAsync("status", 100, $"Prijavljivanje završeno");

                return null;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public override async Task<PersonErrorDto> RegisterPerson(Person person, DateTime? checkInDate, DateTime? checkOutDate)
        {
            return null;
        }


        public string sendOne2Mup(MnePerson p, user user, X509Certificate2 cert, bool obrisan)
        {
            _logger.LogDebug("PRIJAVA NULL: " + (p == null).ToString() + ", RB NULL: " + (_rb90client == null).ToString() + ", USER NULL: " + (user == null).ToString());
            var s = Stranac(p);
            var data = GetString(s);
            try
            {
                if (p.ExternalId == null)
                {
                    var addRequest = new addPersonRequest(s, _rb90User.id, Sign(data), data);
                    var result = _rb90client!.addPerson(addRequest);
                    if (result.id != 0)
                    {
                        p.ExternalId = result.id;
                        p.Error = null;
                    }
                    else
                    {
                        return result.result;
                    }
                    _db.SaveChanges();
                }
                else
                {
                    var editRequest = new editPersonRequest(s, _rb90User.id, Sign(data), data);
                    var result = _rb90client.editPerson(editRequest);
                    if (result.id != 0)
                    {
                        p.ExternalId = result.id;
                        p.Error = null;
                    }
                    else
                    {
                        return result.result;
                    }
                    _db.SaveChanges();
                }
                return null;
            }
            catch (Exception e)
            {
                _logger.LogDebug("SEND2MUP ERROR - ID: " + p.Id.ToString() + ", Gost: " + p.FirstName + " " + p.LastName + " RB: " + Exceptions.StringException(e));

                return e.Message;
            }
        }

        public string Sign(string to_sign)
        {
            var data = Encoding.UTF8.GetBytes(to_sign);
            var hash = new SHA1Managed().ComputeHash(data);
            var rsa = new RSAPKCS1SignatureFormatter(_cert.PrivateKey);
            rsa.SetHashAlgorithm("SHA1");
            var sign_val = rsa.CreateSignature(hash);
            return Convert.ToBase64String(sign_val);
        }

        public string GetString(stranac item)
        {
            var user = _rb90User;
            DateTime min = new DateTime(1, 1, 1);

            string r = "";

            r += item.tipGosta.id + ",";
            r += item.maticniBroj + ",";
            r += item.prezime + ",";
            r += item.ime + ",";
            r += item.pol + ",";
            r += item.datumRodjenja.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ",";
            r += item.drzavaRodjenja.kod + ",";
            r += item.gradRodjenja + ",";
            r += item.drzavljanstvo.kod + ",";
            r += item.drzavaIzdavanjaDokumenta.kod + ",";
            r += item.vrstaJavneIsprave.id + ",";
            r += item.brojJavneIsprave + ",";
            r += item.izdavaocJavneIsprave + ",";
            r += item.rokVazenjaIsprave.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ",";

            if (item.vrstaVize != null) r += item.vrstaVize + ",";
            if (item.brojVize != null) r += item.brojVize + ",";
            if (item.mjestoIzdavanjaVize != null) r += item.mjestoIzdavanjaVize + ",";
            if (item.rokVazenjaVizeOd != min) r += item.rokVazenjaVizeOd.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ","; else r += ",";
            if (item.rokVazenjaVizeDo != min) r += item.rokVazenjaVizeDo.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ","; else r += ",";
            if (item.datumUlaskaUCG != min) r += item.datumUlaskaUCG.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ","; else r += ",";
            if (item.mjestoUlaskaUCG != null) r += item.mjestoUlaskaUCG.id.ToString() + ","; else r += ",";
            if (item.opstinaBoravka != null) r += item.opstinaBoravka.kod + ",";
            if (item.mjestoBoravka != null) r += item.mjestoBoravka.kod + ",";
            if (item.adresaBoravkaiKucniBroj != null) r += item.adresaBoravkaiKucniBroj + ",";
            if (item.prezimeKorisnikaObjekta != null) r += item.prezimeKorisnikaObjekta + ",";
            if (item.imeKorisnikaObjekta != null) r += item.imeKorisnikaObjekta + ",";
            if (item.jmbgKorisnikaObjekta != null) r += item.jmbgKorisnikaObjekta + ",";

            r += item.datumPrijave.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ",";
            if (item.datumOdjave != min) r += item.datumOdjave.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ","; else r += ",";
            r += item.drzavaPrebivalista.kod + ",";
            if (item.gradPrebivalista != null) r += item.gradPrebivalista + ",";
            if (item.adresaiBrojPrebivalista != null) r += item.adresaiBrojPrebivalista + ",";
            r += item.davaocSmjestaja.id.ToString() + ",";
            r += user.id.ToString() + ",";
            r += item.obrisan.ToString() + ",";

            return r.ToUpper();
        }

        public stranac Stranac(MnePerson p)
        {
            //SISTEM.Models.Helpers.Logger.Debug("Stranac before lookups");

            var user = _rb90User;

            var so = _db.Properties.Where(a => a.Id == p.PropertyId).FirstOrDefault();
            var tg = _db.CodeLists.Where(a => a.Type == "gost" && a.ExternalId == p.PersonType).FirstOrDefault();
            var pr = _db.CodeLists.Where(a => a.Type == "prelaz" && a.ExternalId == p.EntryPoint).FirstOrDefault();
            var ji = _db.CodeLists.Where(a => a.Type == "isprava" && a.ExternalId == p.DocumentType).FirstOrDefault();
            var d = _db.CodeLists.Where(a => a.Type == "drzava" && a.ExternalId == p.Nationality).FirstOrDefault();
            var dr = _db.CodeLists.Where(a => a.Type == "drzava" && a.ExternalId == p.BirthCountry).FirstOrDefault();
            var di = _db.CodeLists.Where(a => a.Type == "drzava" && a.ExternalId == p.DocumentCountry).FirstOrDefault();
            var dp = _db.CodeLists.Where(a => a.Type == "drzava" && a.ExternalId == p.PermanentResidenceCountry).FirstOrDefault();

            //SISTEM.Models.Helpers.Logger.Debug("Stranac after lookups");

            stranac s = new stranac();

            s.tipGosta = new tipGosta(); s.tipGosta.id = int.Parse(tg.ExternalId); s.tipGosta.naziv = tg.Name;
            s.maticniBroj = p.PersonalNumber;
            s.prezime = p.LastName;
            s.ime = p.FirstName;
            s.pol = p.Gender;
            s.datumRodjenja = p.BirthDate;
            s.drzavaRodjenja = new drzava(); s.drzavaRodjenja.kod = dr.ExternalId; s.drzavaRodjenja.naziv = dr.Name;
            s.gradRodjenja = p.BirthPlace;
            s.drzavljanstvo = new drzava(); s.drzavljanstvo.kod = d.ExternalId; s.drzavljanstvo.naziv = d.Name;
            s.drzavaIzdavanjaDokumenta = new drzava(); s.drzavaIzdavanjaDokumenta.kod = di.ExternalId; s.drzavaIzdavanjaDokumenta.naziv = di.Name;
            s.vrstaJavneIsprave = new javnaIsprava(); s.vrstaJavneIsprave.id = int.Parse(ji.ExternalId); s.vrstaJavneIsprave.naziv = ji.Name;
            s.brojJavneIsprave = p.DocumentNumber;
            s.izdavaocJavneIsprave = p.DocumentIssuer;
            s.rokVazenjaIsprave = p.DocumentValidTo;
            s.vrstaVize = p.VisaType;
            s.brojVize = p.VisaNumber;
            s.mjestoIzdavanjaVize = p.VisaIssuePlace;

            if (p.VisaValidFrom.HasValue) s.rokVazenjaVizeOd = p.VisaValidFrom.Value;
            if (p.VisaValidTo.HasValue) s.rokVazenjaVizeDo = p.VisaValidTo.Value;
            if (p.EntryPointDate.HasValue) s.datumUlaskaUCG = p.EntryPointDate.Value;
            if (pr != null) { s.mjestoUlaskaUCG = new granicniPrelaz(); s.mjestoUlaskaUCG.id = int.Parse(pr.ExternalId); s.mjestoUlaskaUCG.naziv = pr.Name; } else s.mjestoUlaskaUCG = null;
            s.opstinaBoravka = null;
            s.mjestoBoravka = null;
            s.adresaBoravkaiKucniBroj = null;
            s.prezimeKorisnikaObjekta = null;
            s.imeKorisnikaObjekta = null;
            s.jmbgKorisnikaObjekta = null;
            s.datumPrijave = p.CheckIn;
            if (p.CheckOut.HasValue) s.datumOdjave = p.CheckOut.Value;
            s.drzavaPrebivalista = new drzava(); s.drzavaPrebivalista.kod = dp.ExternalId; s.drzavaPrebivalista.naziv = dp.Name; ;
            s.gradPrebivalista = p.PermanentResidencePlace;
            s.adresaiBrojPrebivalista = p.PermanentResidenceAddress;


            s.davaocSmjestaja = new smjestajniObjekat(); s.davaocSmjestaja.id = so.ExternalId; s.davaocSmjestaja.naziv = so.Name ?? so.Name;
            s.azurirao = user;
            s.obrisan = p.IsDeleted;

            if (p.ExternalId != null) s.id = p.ExternalId.Value;

            return s;
        }        

        /*
        public Dictionary<string, List<string>> Check(int grupa)
        {
            var data = _db.MnePersons.Where(a => a.GroupId == grupa).ToList();

            var now = DateTime.Now;

            var errors = new Dictionary<string, List<string>>();

            foreach (var p in data)
            {
                var er = new KeyValuePair<string, List<string>>(p.Id.ToString(), new List<string>());

                if (p.DocumentValidTo < now) er.Value.Add("Datum važenja ličnog dokumenta mora biti u budućnosti");
                if (p.BirthDate.Date >= now) er.Value.Add("Datum rođenja mora biti u prošlosti");
                if (p.CheckIn.Date > now) er.Value.Add("Datum prijave ne smije biti u budućnosti");
                if (p.CheckOut.HasValue && p.CheckOut.Value.Date < p.CheckIn.Date) er.Value.Add("Datum odjave mora biti nakon datuma prijave");
                if (p.ExternalId != null && p.CheckOut.HasValue && p.CheckOut.Value.Date < now.Date) er.Value.Add("Neaktivan boravak se ne može mijenjati");
                if (string.IsNullOrEmpty(p.FirstName)) er.Value.Add("Nijeste unijeli ime gosta");
                if (string.IsNullOrEmpty(p.LastName)) er.Value.Add("Nijeste unijeli prezime gosta");
                if (string.IsNullOrEmpty(p.Nationality)) er.Value.Add("Nijeste unijeli državljanstvo gosta");
                if (string.IsNullOrEmpty(p.PersonalNumber)) er.Value.Add("Nijeste unijeli matični broj gosta");
                if (string.IsNullOrEmpty(p.Gender)) er.Value.Add("Nijeste odabrali pol gosta");
                if (string.IsNullOrEmpty(p.BirthPlace)) er.Value.Add("Nijeste unijeli mjesto rođenja gosta");
                if (string.IsNullOrEmpty(p.BirthCountry)) er.Value.Add("Nijeste unijeli državu rođenja gosta");
                if (string.IsNullOrEmpty(p.PermanentResidencePlace)) er.Value.Add("Nijeste unijeli mjesto prebivališta gosta");
                if (string.IsNullOrEmpty(p.PermanentResidenceAddress)) er.Value.Add("Nijeste unijeli adresu prebivališta gosta");
                if (string.IsNullOrEmpty(p.PermanentResidenceCountry)) er.Value.Add("Nijeste unijeli državu prebivališta gosta");
                if (string.IsNullOrEmpty(p.DocumentType)) er.Value.Add("Nijeste unijeli vrstu ličnog dokumenta gosta");
                if (string.IsNullOrEmpty(p.DocumentNumber)) er.Value.Add("Nijeste unijeli broj ličnog dokumenta gosta");
                if (string.IsNullOrEmpty(p.DocumentCountry)) er.Value.Add("Nijeste unijeli državu izdavanja ličnog dokumenta gosta");
                if (string.IsNullOrEmpty(p.DocumentIssuer)) er.Value.Add("Nijeste unijeli izdavaoca ličnog dokumenta gosta");

                if (er.Value.Any()) errors.Add(er.Key, er.Value);
            }

            return errors;
        }
        */


        public async Task<Stream> ResTaxPdf(int id)
        {
            var docxProvider = new DocxFormatProvider();
            var pdfProvider = new PdfFormatProvider();
            RadFlowDocument document;

            var path = _webHostEnvironment.WebRootPath + "/templates/test.docx";

            using (Stream input = File.OpenRead(path))
            {
                document = docxProvider.Import(input);
            }

            RadFlowDocumentEditor editor = new RadFlowDocumentEditor(document);
            editor.ReplaceText("Point", "trtvrt");
            editor.ReplaceText("<Expr3>", "");

            /*
            var bookmarks = document.EnumerateChildrenOfType<BookmarkRangeStart>();

            foreach (var b in bookmarks)
            {
                int index = b.Paragraph.Inlines.IndexOf(b);
                b.Paragraph.Inlines.Insert(index + 1, new Run(document) { Text = "In bookmark" });
            }
            */
            var stream = new MemoryStream();
            pdfProvider.Export(document, stream);        
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public async Task ResTaxEmail(int id, string from, string email)
        {
            var tax = _db.ResTaxes.FirstOrDefault(a => a.Id == id);
            var obj = _db.Properties.FirstOrDefault(a => a.Id == tax.PropertyId);
            var pdfStream = await ResTaxPdf(id);
            var template = _configuration["SendGrid:Templates:rb90ResTax"]!;
            await _eMailService.SendMail(from, email, template, new
            {
                subject = $@"donotreply: Prijava boravišne takse",
                body = $"Poštovani,\n\nU prilogu se nalazi prijava boravišne takse za period od {tax.DateFrom.ToString("dd.MM.yyyy")} od {tax.DateTo.ToString("dd.MM.yyyy")} za smještajni objekat {obj.Name}.\n\nSrdačan pozdrav,"
            }, ("Boravišna taksa.pdf", pdfStream));            
        }

        public void CalcResTax(ResTax tax, int objekat, DateTime OD, DateTime DO, string vrsta, string tip_gosta)
        {            
            try
            {
                var obj = _db.Properties.FirstOrDefault(a => a.Id == objekat);
                var firma = obj!.LegalEntityId;
                var last_date = DO;
                var limit18 = last_date.AddYears(-18);
                var btx = new ResTaxItem();

                var data = _db.MnePersons
                    .Where(a => a.LegalEntityId == firma && a.PropertyId == obj.Id)
                    .Where(a => a.CheckIn.Date >= OD && a.CheckIn.Date <= DO);

                if (tip_gosta == "STRANI") data = data.Where(a => a.PersonType != "1");
                if (tip_gosta == "DOMACI") data = data.Where(a => a.PersonType == "1");

                if (vrsta == "FULL") data = data.Where(a => EF.Functions.DateDiffYear(a.BirthDate, DO) >= 18);
                if (vrsta == "HALF") data = data.Where(a => EF.Functions.DateDiffYear(a.BirthDate, DO) < 18 && EF.Functions.DateDiffYear(a.BirthDate, DO) >= 12);
                if (vrsta == "NONE") data = data.Where(a => EF.Functions.DateDiffYear(a.BirthDate, DO) < 12);

                var lica = data.Count();
                var noc = data.Select(a => _db.Nights(a.Id, DO)).DefaultIfEmpty(0).Sum();

                var iznos_tax = obj.ResidenceTax ?? 0;

                btx.NumberOfGuests = lica;
                btx.NumberOfNights = noc;
                btx.TaxPerNight = vrsta == "FULL" ? iznos_tax : vrsta == "HALF" ? iznos_tax / 2m : 0m;
                btx.TotalTax = btx.TaxPerNight * noc;
                btx.TaxType = vrsta;
                btx.GuestType = tip_gosta;

                tax.Items.Add(btx);

                _db.SaveChanges();
            }
            catch (Exception e)
            {

            }
        }

        public override async Task<List<CodeList>> CodeLists()
        {
            return _db.CodeLists.Where(a => a.Country == "MNE").ToList();
        }

        public override async Task<object> Authenticate()
        {
            try
            {
                _cert = new X509Certificate2(_legalEntity.Rb90CertData, _legalEntity.Rb90Password);
                var issuer = _cert.Issuer.Split(new char[] { ',' });
                var cn = issuer.Where(a => a.Trim().StartsWith("CN")).FirstOrDefault();
                cn = cn!.Split(new char[] { '=' })[1].Trim();
                var CertUser = _cert.GetSerialNumberString() + "@" + cn;
                var authResponse = await _rb90client.authenticateAsync(new authenticateRequest(CertUser));
                _rb90User = authResponse.user;               

                return (authResponse.user, authResponse.listaSmjestajnihObjekata, _cert);
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR MNE: Authenticate {e.Message}");
                throw;
            }
        }

        public override async Task CertificateMail(Group group, string email)
        {
            var template = _configuration["SendGrid:Templates:rb90Confirmation"];
            var pdfStream = await CertificatePdf(group);
            await _eMailService.SendMail(_user.Email, email ?? group.Email, template, new
            {
                subject = $@"donotreply: Potvrde o prijavi boravka",
                body = $"Poštovani,\n\nU prilogu se nalaze potvrde o prijavi boravka.\n\nSrdačan pozdrav,"
            }, ("Potvrde.pdf", pdfStream));
        }

        public override async Task<Stream> CertificatePdf(Group group)
        {
            return null;
        }

        public override async Task SendGuestToken(int propertyId, int? unitId, string email, string phoneNo, string lang)
        {
            throw new NotImplementedException();
        }

        public override async Task<Person> Person(object person)
        {
            MnePerson mnePerson;
            var dto = person as MnePersonDto;

            if (dto.Id == 0)
            {
                mnePerson = new MnePerson();
                dto.Guid = Guid.NewGuid().ToString();
                _db.MnePersons.Add(mnePerson);
            }
            else
            {
                mnePerson = _db.MnePersons.FirstOrDefault(a => a.Id == dto.Id)!;
            }

            dto.LegalEntityId = _user.LegalEntityId;

            _mapper.Map(dto, mnePerson);

            _db.SaveChanges();

            return mnePerson;
        }

        public override List<PersonErrorDto> Validate(Group group, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var result = new List<PersonErrorDto>();
            _db.Entry(group).Collection(a => a.Persons).Load();
            foreach (MnePerson p in group.Persons)
            {
                var one = Validate(p, checkInDate, checkOutDate);
                if (one.Errors.Any()) result.Add(one);
            }
            return result;
        }

        public override PersonErrorDto Validate(Person person, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var p = person as MnePerson;
            var err = new PersonErrorDto();
            var now = DateTime.Now;

            if (p.DocumentValidTo < now) err.Errors.Add(new PersonError() { Field = nameof(p.DocumentValidTo), Error = "Datum važenja ličnog dokumenta mora biti u budućnosti." });
            if (p.BirthDate.Date >= now) err.Errors.Add(new PersonError() { Field = nameof(p.BirthDate), Error = "Datum rođenja mora biti u prošlosti." });
            if (p.CheckIn.Date >= now) err.Errors.Add(new PersonError() { Field = nameof(p.CheckIn), Error = "Datum prijave ne smije biti u budućnosti." });
            if (p.CheckOut.HasValue && p.CheckOut.Value.Date < p.CheckIn.Date) err.Errors.Add(new PersonError() { Field = nameof(p.CheckIn), Error = "Datum odjave mora biti nakon datuma prijave." });
            if (p.ExternalId != null && p.CheckOut.HasValue && p.CheckOut.Value.Date < now.Date) err.Errors.Add(new PersonError() { Field = nameof(p.ExternalId), Error = "Neaktivan boravak se ne može mijenjati." });
            if (string.IsNullOrEmpty(p.FirstName)) err.Errors.Add(new PersonError() { Field = nameof(p.FirstName), Error = "Morate unijeti ime gosta." });
            if (string.IsNullOrEmpty(p.LastName)) err.Errors.Add(new PersonError() { Field = nameof(p.LastName), Error = "Morate unijeti prezime gosta." });
            if (string.IsNullOrEmpty(p.Nationality)) err.Errors.Add(new PersonError() { Field = nameof(p.Nationality), Error = "Morate unijeti državljanstvi gosta." });
            if (string.IsNullOrEmpty(p.PersonalNumber)) err.Errors.Add(new PersonError() { Field = nameof(p.PersonalNumber), Error = "Morate unijeti matični broj gosta." });
            if (string.IsNullOrEmpty(p.Gender)) err.Errors.Add(new PersonError() { Field = nameof(p.Gender), Error = "Morate unijeti pol gosta." });
            if (string.IsNullOrEmpty(p.BirthPlace)) err.Errors.Add(new PersonError() { Field = nameof(p.BirthPlace), Error = "Morate unijeti mjesto rođenja gosta." });
            if (p.BirthDate == null) err.Errors.Add(new PersonError() { Field = nameof(p.BirthDate), Error = "Morate unijeti državu rođenja gosta." });
            if (string.IsNullOrEmpty(p.PermanentResidencePlace)) err.Errors.Add(new PersonError() { Field = nameof(p.PermanentResidencePlace), Error = "Morate unijeti mjesto prebivališta gosta." });
            if (string.IsNullOrEmpty(p.PermanentResidenceAddress)) err.Errors.Add(new PersonError() { Field = nameof(p.PermanentResidenceAddress), Error = "Morate unijeti adresu prebivališta gosta." });
            if (string.IsNullOrEmpty(p.PermanentResidenceCountry)) err.Errors.Add(new PersonError() { Field = nameof(p.PermanentResidenceCountry), Error = "Morate unijeti državu prebivališta gosta." });
            if (string.IsNullOrEmpty(p.DocumentType)) err.Errors.Add(new PersonError() { Field = nameof(p.DocumentType), Error = "Morate unijeti vrstu ličnog dokumenta gosta." });            
            if (string.IsNullOrEmpty(p.DocumentNumber)) err.Errors.Add(new PersonError() { Field = nameof(p.DocumentNumber), Error = "Morate unijeti broj ličnog dokumenta gosta." });
            if (string.IsNullOrEmpty(p.DocumentCountry)) err.Errors.Add(new PersonError() { Field = nameof(p.DocumentCountry), Error = "Morate unijeti državu izdavanja ličnog dokumenta gosta." });
            if (string.IsNullOrEmpty(p.DocumentIssuer)) err.Errors.Add(new PersonError() { Field = nameof(p.DocumentIssuer), Error = "Morate unijeti izdavaoca ličnog dokumenta gosta." });

            return err;
        }

        public async override Task<object> Properties()
        {
            (user user, smjestajniObjekat[] so, X509Certificate2 cert) = ((user, smjestajniObjekat[], X509Certificate2)) await Authenticate();
            try
            {
                foreach (var s in so)
                {
                    if (_db.Properties.Any(a => a.LegalEntityId == _user.LegalEntityId && a.ExternalId == s.id) == false)
                    {
                        var property = new Property();
                        property.LegalEntityId = _user.LegalEntityId;
                        property.ExternalId = s.id;
                        property.Type = "";
                        property.Address = "";
                        property.Municipality = "";
                        property.RegNumber = "";
                        property.Status = "";
                        property.Name = s.naziv;
                        property.PropertyName = s.naziv;
                        _db.Properties.Add(property);
                        _db.SaveChanges();
                    }
                }

                return so;
            }
            catch (Exception e)
            {
                _logger.LogError($"ERROR MNE: Properties {e.Message}");                
                throw;
            }
        }
    }
}
