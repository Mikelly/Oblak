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
using Oblak.Models.EFI;
using System.Runtime.ConstrainedExecution;
using Oblak.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Oblak.SignalR;
using Oblak.Models.Api;
using Oblak.Services.SRB.Models;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using Oblak.Data.Enums;
using Telerik.Documents.Common.Model;
using Telerik.Windows.Documents.Flow.Model.Styles;
using Oblak.Services.Reporting;
using Humanizer;

namespace Oblak.Services.MNE
{
    public class MneClient : Register
    {
        protected readonly ILogger<MneClient> _logger;
        protected readonly mup _rb90client;
        protected IMapper _mapper;
        protected ApplicationUser _user;
        protected X509Certificate2 _cert;
        protected user _rb90User;
        protected HttpContext _context;

        public MneClient(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<MneClient> logger,
            mup rb90client,
            ApplicationDbContext db,
            SelfRegisterService selfRegisterService,
            ReportingService reporting,
            eMailService eMailService,
            IHubContext<MessageHub> messageHub,
            IWebHostEnvironment webHostEnvironment) : base(configuration, eMailService, reporting, selfRegisterService, webHostEnvironment, messageHub, db)
        {
            _logger = logger;
            _rb90client = rb90client;
            _context = httpContextAccessor.HttpContext;

            var username = _context.User.Identity!.Name;
            if (username != null)
            {
                _user = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username)!;
                if (_user != null) _legalEntity = _user.LegalEntity;
            }
        }

        public (user, smjestajniObjekat[], X509Certificate2) Auth(LegalEntity legalEntity)
        {
            _legalEntity = legalEntity;

            var cert = new X509Certificate2(legalEntity.Rb90CertData, legalEntity.Rb90Password);
            var issuer = cert.Issuer.Split(new char[] { ',' });
            var cn = issuer.Where(a => a.Trim().StartsWith("CN")).FirstOrDefault();
            cn = cn!.Split(new char[] { '=' })[1].Trim();
            var CertUser = cert.GetSerialNumberString() + "@" + cn;
            var authResponse = _rb90client.authenticate(new authenticateRequest(CertUser));

            _cert = cert;
            _rb90User = authResponse.user;

            return (authResponse.user, authResponse.listaSmjestajnihObjekata, cert);
        }

        public override async Task<List<PersonErrorDto>> RegisterGroup(Group group, DateTime? checkInDate, DateTime? checkOutDate)
        {
            try
            {
                var retval = new List<PersonErrorDto>();

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

                if (_legalEntity.Test)
                {
                    foreach (var pr in data) pr.ExternalId = 0;
                    _db.SaveChanges();
                    return null;
                }

                int total = data.Count();

                try
                {
                    (user, so, cert) = ((user, smjestajniObjekat[], X509Certificate2))await Authenticate(group.LegalEntity);
                    await Task.Delay(100);
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
                    if (_context != null)
                    {
                        MessageHub._connectedUsers.ForEach(val =>
                        {
                            if (val.Username == _context.User.Identity!.Name!)
                            {
                                _messageHub.Clients.Client(val.ConnectionId).SendAsync(
                                    "status",
                                    (int)(Math.Round((decimal)c / (decimal)total * 100m, 0, MidpointRounding.AwayFromZero)),
                                    $"Prijavljivanje gostiju {c}/{total}",
                                    $"{pr.FirstName} {pr.LastName}");
                            }
                        });
                    }
                    var error = sendOne2Mup(pr, false);
                    if (error != null)
                    {
                        pr.Error = error;
                        retval.Add(new PersonErrorDto() { PersonId = $"{pr.FirstName} {pr.LastName}", ExternalErrors = new List<string>() { error } });
                    }
                    _db.SaveChanges();
                    await Task.Delay(100);
                }

                if (_context != null)
                {
                    MessageHub._connectedUsers.ForEach(val =>
                    {
                        if (val.Username == _context.User.Identity!.Name!)
                        {
                            _messageHub.Clients.Client(val.ConnectionId).SendAsync("status", 100, $"Prijavljivanje gostiju završeno", $"");
                        }
                    });

                    //await _messageHub.Clients.All/*.User(_context.User.Identity!.Name!)*/.SendAsync("status", 100, $"Prijavljivanje završeno");
                }

                return retval;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public override async Task<PersonErrorDto> RegisterPerson(Person person, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var mnePerson = person as MnePerson;

            try
            {
                var (user, so, cert) = ((user, smjestajniObjekat[], X509Certificate2))await Authenticate(mnePerson.LegalEntity);
                await Task.Delay(100);
            }
            catch (Exception e)
            {
                _logger.LogDebug("SEND2MUP AUTH ERROR - RB: " + Exceptions.StringException(e));
                throw new Exception("Greška prilikom autentifikacije na RB90 servis.");
            }

            try
            {
                var error = sendOne2Mup(mnePerson, false);
                var errorDto = new PersonErrorDto();
                if (error != null)
                {
                    mnePerson.Error = error;
                    errorDto.PersonId = $"{mnePerson.FirstName} {mnePerson.LastName}";
                    errorDto.ExternalErrors = new List<string> { error };
                }
                _db.SaveChanges();
                return errorDto;
            }
            catch (Exception e)
            {
                throw;
            }
        }


        public string sendOne2Mup(MnePerson p, bool obrisan)
        {
            _logger.LogDebug("PRIJAVA NULL: " + (p == null).ToString() + ", RB NULL: " + (_rb90client == null).ToString() + ", USER NULL: " + (_rb90User == null).ToString());
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
            if (item.davaocSmjestaja != null) r += item.davaocSmjestaja.id.ToString() + ",";
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
            if (p.LegalEntity.PassThroughId.HasValue)
            {
                var op = _db.CodeLists.Where(a => a.Type == "opstina" && a.ExternalId == p.Property!.Municipality!.ExternalId).FirstOrDefault();
                var mj = _db.CodeLists.Where(a => a.Type == "mjesto" && a.ExternalId == p.Property!.Place).FirstOrDefault();

                s.opstinaBoravka = new opstina(); s.opstinaBoravka.kod = p.Property.Municipality.ExternalId; s.opstinaBoravka.naziv = op.ExternalId;
                s.mjestoBoravka = new mjesto(); s.mjestoBoravka.kod = p.Property.Place; s.mjestoBoravka.naziv = mj.ExternalId;
                s.adresaBoravkaiKucniBroj = p.Property.Address;
                var f = "";
                var l = "";
                var sp = p.Property.LegalEntity.Name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                f = sp[0];
                if (sp.Length > 1) l = string.Join(" ", sp.Skip(1));
                s.prezimeKorisnikaObjekta = l;
                s.imeKorisnikaObjekta = f;
                s.jmbgKorisnikaObjekta = p.Property.LegalEntity.TIN;
            }
            else
            {
                s.davaocSmjestaja = new smjestajniObjekat(); s.davaocSmjestaja.id = so.ExternalId; s.davaocSmjestaja.naziv = so.Name ?? so.Name;
            }
            s.datumPrijave = p.CheckIn;
            if (p.CheckOut.HasValue) s.datumOdjave = p.CheckOut.Value;
            s.drzavaPrebivalista = new drzava(); s.drzavaPrebivalista.kod = dp.ExternalId; s.drzavaPrebivalista.naziv = dp.Name; ;
            s.gradPrebivalista = p.PermanentResidencePlace;
            s.adresaiBrojPrebivalista = p.PermanentResidenceAddress;
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

            var path = _env.WebRootPath + "/templates/test.docx";

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
            var tax = _db.ResTaxCalc.FirstOrDefault(a => a.Id == id);
            var obj = _db.Properties.FirstOrDefault(a => a.Id == tax.PropertyId);
            var pdfStream = await ResTaxPdf(id);
            var template = _configuration["SendGrid:Templates:rb90ResTax"]!;
            await _eMailService.SendMail(from, email, template, new
            {
                subject = $@"donotreply: Prijava boravišne takse",
                body = $"Poštovani,\n\nU prilogu se nalazi prijava boravišne takse za period od {tax.DateFrom.ToString("dd.MM.yyyy")} od {tax.DateTo.ToString("dd.MM.yyyy")} za smještajni objekat {obj.Name}.\n\nSrdačan pozdrav,"
            }, ("Boravišna taksa.pdf", pdfStream));
        }

        public void CalcResTax(ResTaxCalc tax, int objekat, DateTime OD, DateTime DO, string vrsta, string tip_gosta)
        {
            try
            {
                var obj = _db.Properties.Include(a => a.Municipality).FirstOrDefault(a => a.Id == objekat);
                var firma = obj!.LegalEntityId;
                var last_date = DO;
                var limit18 = last_date.AddYears(-18);
                var btx = new ResTaxCalcItem();

                var data = _db.MnePersons
                    .Where(a => a.LegalEntityId == firma && a.PropertyId == obj.Id)
                    .Where(a => a.CheckIn.Date >= OD && a.CheckIn.Date <= DO);

                if (tip_gosta == "STRANI") data = data.Where(a => a.PersonType != "1");
                if (tip_gosta == "DOMACI") data = data.Where(a => a.PersonType == "1");

                if (vrsta == "FULL") data = data.Where(a => EF.Functions.DateDiffYear(a.BirthDate, DO) >= 18);
                if (vrsta == "HALF") data = data.Where(a => EF.Functions.DateDiffYear(a.BirthDate, DO) < 18 && EF.Functions.DateDiffYear(a.BirthDate, DO) >= 12);
                if (vrsta == "NONE") data = data.Where(a => EF.Functions.DateDiffYear(a.BirthDate, DO) < 12);

                var lica = data.Count();
                var noc = data.Select(a => _db.Nights(a.Id, DO)).Sum();

                var iznos_tax = obj.Municipality != null ? obj.Municipality.ResidenceTaxAmount : 1;

                btx.NumberOfGuests = lica;
                btx.NumberOfNights = noc;
                btx.TaxPerNight = vrsta == "FULL" ? iznos_tax : vrsta == "HALF" ? iznos_tax / 2m : 0m;
                btx.TotalTax = btx.TaxPerNight * noc;
                btx.TaxType = vrsta;
                btx.GuestType = tip_gosta;

                //tax.Items = new List<ResTaxCalcItem>();
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

        public override async Task<object> Authenticate(LegalEntity? legalEntity = null)
        {
            try
            {
                if (legalEntity != null) _legalEntity = legalEntity;
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


        public override async Task ConfirmationGroupMail(Group group, string email)
        {
            var template = _configuration["REPORTING:MNE:ConfirmationEmailTemplate"];
            var senderEmail = _configuration["SendGrid:EmailAddress"];
            var pdfStream = await ConfirmationGroupPdf(group);
            await _eMailService.SendMail(senderEmail, email ?? group.Email, template, new
            {
                subject = $@"donotreply: Potvrde o prijavi boravka",
                message = $"U prilogu se nalaze potvrde o prijavi boravka.",
                sender = group.Property.LegalEntity.Name
            }, ("Potvrde.pdf", pdfStream));
        }


        public override async Task ConfirmationPersonMail(Person person, string email)
        {
            var template = _configuration["REPORTING:MNE:ConfirmationEmailTemplate"];
            var senderEmail = _configuration["SendGrid:EmailAddress"];
            var pdfStream = await ConfirmationPersonPdf(person);
            await _eMailService.SendMail(senderEmail, email, template, new
            {
                subject = $@"donotreply: Potvrda o prijavi boravka",
                message = $"U prilogu se nalazi potvrda o prijavi boravka",
                sender = person.Property.LegalEntity.Name
            }, ("Potvrda.pdf", pdfStream));
        }


        public override async Task<Stream> ConfirmationGroupPdf(Group group)
        {
            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var reportName = _configuration["REPORTING:MNE:Confirmation"];
            var report = _db.Reports.FirstOrDefault(a => a.Name == reportName);
            var path = Path.Combine(_env.ContentRootPath, "Reports", report.Path);

            reportSource.Uri = path;
            reportSource.Parameters.Add("group", group.Id);
            reportSource.Parameters.Add("person", 0);

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

            if (!result.HasErrors)
            {
                return new MemoryStream(result.DocumentBytes);
            }
            else
            {
                return null;
            }
        }


        public override async Task<Stream> ConfirmationPersonPdf(Person person)
        {
            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var reportName = _configuration["REPORTING:MNE:Confirmation"];
            var report = _db.Reports.FirstOrDefault(a => a.Name == reportName);
            var path = Path.Combine(_env.ContentRootPath, "Reports", report.Path);

            reportSource.Uri = path;
            reportSource.Parameters.Add("group", 0);
            reportSource.Parameters.Add("person", person.Id);

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

            if (!result.HasErrors)
            {
                return new MemoryStream(result.DocumentBytes);
            }
            else
            {
                return null;
            }
        }

        public override async Task SendGuestToken(int propertyId, int? unitId, string email, string phoneNo, string lang)
        {
            throw new NotImplementedException();
        }

        public override async Task<Person> Person(object person)
        {
            try
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

                _logger.LogDebug("Mne Person Legal Entity:" + _legalEntity.Id);

                mnePerson.LegalEntityId = _legalEntity.Id;
                //mnePerson.Id = dto.Id;
                //mnePerson.ExternalId = dto.ExternalId;
                //mnePerson.Guid = dto.Guid;
                //mnePerson.LegalEntityId = dto.LegalEntity.Id;
                //mnePerson.LegalEntityName = dto.LegalEntityName;
                //mnePerson.PropertyExternalId = dto.PropertyExternalId;
                mnePerson.PropertyId = dto.PropertyId;
                mnePerson.GroupId = dto.GroupId;
                //mnePerson.UnitId = dto.UnitId;
                mnePerson.LastName = dto.LastName;
                mnePerson.FirstName = dto.FirstName;
                mnePerson.PersonalNumber = dto.PersonalNumber;
                mnePerson.Gender = dto.Gender;
                mnePerson.BirthDate = dto.BirthDate;
                mnePerson.IsDeleted = dto.IsDeleted;
                mnePerson.Status = dto.Status;
                mnePerson.Error = dto.Error;
                mnePerson.BirthPlace = dto.BirthPlace;
                mnePerson.BirthCountry = dto.BirthCountry;
                mnePerson.Nationality = dto.Nationality;
                mnePerson.PersonType = dto.PersonType;
                mnePerson.PermanentResidenceCountry = dto.PermanentResidenceCountry;
                mnePerson.PermanentResidencePlace = dto.PermanentResidencePlace;
                mnePerson.PermanentResidenceAddress = dto.PermanentResidenceAddress;
                //mnePerson.ResidencePlace = dto.ResidencePlace;
                //mnePerson.ResidenceAddress = dto.ResidenceAddress;
                mnePerson.CheckIn = dto.CheckIn.Date;
                mnePerson.CheckOut = dto.CheckOut;
                mnePerson.DocumentType = dto.DocumentType;
                mnePerson.DocumentNumber = dto.DocumentNumber;
                mnePerson.DocumentValidTo = dto.DocumentValidTo;
                mnePerson.DocumentCountry = dto.DocumentCountry;
                mnePerson.DocumentIssuer = dto.DocumentIssuer;
                mnePerson.VisaType = dto.VisaType;
                mnePerson.VisaNumber = dto.VisaNumber;
                mnePerson.VisaValidFrom = dto.VisaValidFrom;
                mnePerson.VisaValidTo = dto.VisaValidTo;
                mnePerson.VisaIssuePlace = dto.VisaIssuePlace;
                mnePerson.EntryPoint = dto.EntryPoint;
                mnePerson.EntryPointDate = dto.EntryPointDate;
                mnePerson.Other = dto.Other;
                mnePerson.ResTaxTypeId = dto.ResTaxTypeId;
                mnePerson.ResTaxPaymentTypeId = dto.ResTaxPaymentTypeId;
                mnePerson.ResTaxExemptionTypeId = dto.ResTaxExemptionTypeId;
                mnePerson.ResTaxAmount = dto.ResTaxAmount;
                mnePerson.Note = dto.Note;
                mnePerson.ComputerCreatedId = dto.ComputerCreatedId;

                if (_user != null && mnePerson.CheckInPointId == null)
                {
                    mnePerson.CheckInPointId = _user.CheckInPointId;
                }

                var partner = _db.Properties.Include(x => x.LegalEntity).ThenInclude(x => x.Partner).Where(x => x.Id == dto.PropertyId).FirstOrDefault().LegalEntity.Partner;

                // Ako nije definisan tip gosta za plaćanje, zaključi ga na osnovu datuma rođenja                
                // if (mnePerson.ResTaxTypeId == null)
                {
                    var zero = new DateTime(1, 1, 1);
                    var span = (DateTime.Now.Date - mnePerson.BirthDate.Date);
                    var age = (zero + span).Year - 1;

                    var resTaxType = _db.ResTaxTypes.Where(a => a.PartnerId == partner.Id).FirstOrDefault(a => a.AgeFrom <= age && age <= a.AgeTo);

                    if (resTaxType != null)
                    {
                        mnePerson.ResTaxTypeId = resTaxType.Id;
                    }
                }
                _db.Entry(mnePerson).Reference(a => a.ResTaxType).Load();
                 
                //Ako je gost clan posade broda (grupna prijava) ukljucujuci vlasnika taksa je 0e i fee se ne racuna
                if (_db.Groups.Any(g => g.Id == mnePerson.GroupId && g.VesselId.HasValue))
                {
                    mnePerson.ResTaxTypeId = null;
                    mnePerson.ResTaxAmount = 0;
                    mnePerson.ResTaxFee = 0;
                }  
                // Ako je već definisan tip gosta za plaćanje                
                else if (mnePerson.ResTaxTypeId != null)
                {
                    // Ovo je da eliminišemo ako je oslobođenje
                    if (new List<int>() { 1, 2, 3, 27, 28, 29 }.Contains(mnePerson.ResTaxTypeId.Value) == false || mnePerson.ResTaxExemptionTypeId != null)
                    {
                        mnePerson.ResTaxAmount = 0;
                        mnePerson.ResTaxFee = 0;
                    }
                    else
                    { 
                        // Nađemo tip gosta za plaćanje i sračunamo mu taksu 
                        var resTaxType = _db.ResTaxTypes.FirstOrDefault(a => a.Id == mnePerson.ResTaxTypeId);
                        if (mnePerson.CheckOut.HasValue)
                        {
                            var days = (int)(mnePerson.CheckOut.Value.Date - mnePerson.CheckIn.Date).TotalDays;
                            mnePerson.ResTaxAmount = resTaxType.Amount * days;
                        }

                        // Sad računamo proviziju za gosta.
                        // Prvo ako je individualni gost, onda imamo što računati
                        if (mnePerson.ResTaxPaymentTypeId != null && mnePerson.GroupId == null)
                        {
                            // Nađemo način plaćanja
                            var resTaxPayment = _db.ResTaxPaymentTypes.Include(a => a.PaymentFees).FirstOrDefault(a => a.Id == mnePerson.ResTaxPaymentTypeId);

                            // Ako taj način plaćanja ima proviziju, i nju računamo
                            if (resTaxPayment.PaymentFees.Any())
                            {
                                var resTaxFee = resTaxPayment.PaymentFees.Where(a => a.LowerLimit <= mnePerson.ResTaxAmount && mnePerson.ResTaxAmount <= a.UpperLimit).FirstOrDefault();
                                if (resTaxFee != null)
                                {
                                    if (resTaxFee.FeeAmount.HasValue) mnePerson.ResTaxFee = resTaxFee.FeeAmount.Value;
                                    if (resTaxFee.FeePercentage.HasValue) mnePerson.ResTaxFee = resTaxFee.FeePercentage.Value / 100m * mnePerson.ResTaxAmount;
                                }
                            }
                            // A, ako nema, onda je provizija 0.
                            else
                            {
                                mnePerson.ResTaxFee = 0;
                            }
                        }
                        // A, ako je gost iz grupe, odma mu je provizija 0, sračunaće se na nivou grupe
                        else
                        {
                            mnePerson.ResTaxFee = 0;
                        }
                    } 
                } 

                _db.SaveChanges();

                // Ako ne znamo koji je tip gosta za plaćanje, prvo to sračunamo
                // Ovo mi možda prvo trebao, ali to ću poslije, kada dodam kolonu za tip gosta za plaćanje
                //else
                //{
                //    var zero = new DateTime(1, 1, 1);
                //    var span = (DateTime.Now.Date - mnePerson.BirthDate.Date);
                //    var age = (zero + span).Year - 1;

                //    var resTaxType = _db.ResTaxTypes.Where(a => a.PartnerId == partner.Id).FirstOrDefault(a => a.AgeFrom <= age && age <= a.AgeTo);
                //    if (resTaxType != null)
                //    {
                //        mnePerson.ResTaxTypeId = resTaxType.Id;

                //        if (mnePerson.CheckOut.HasValue)
                //        {
                //            var days = (int)(mnePerson.CheckOut.Value.Date - mnePerson.CheckIn.Date).TotalDays;
                //            if (days < 0) days = 0;
                //            mnePerson.ResTaxAmount = resTaxType.Amount * days;
                //        }

                //        var resPaymentType = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == partner.Id).FirstOrDefault(a => a.PaymentStatus == TaxPaymentStatus.None);
                //        mnePerson.ResTaxPaymentTypeId = resPaymentType?.Id;
                //    }
                //}

                // Na kraju, računamo taksu i proviziju grupe

                                
                if (mnePerson.GroupId != null)
                {
                    _db.Entry(mnePerson).Reference(a => a.Group).Load();
                    _db.Entry(mnePerson.Group!).Reference(a => a.ResTaxPaymentType).Load();
                    this.CalcGroupResTax(mnePerson.Group, mnePerson.Group?.ResTaxPaymentType?.PaymentStatus ?? TaxPaymentStatus.None);
                }

                _db.SaveChanges();

                return mnePerson;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public void CalcGroupResTax(Group g, TaxPaymentStatus pay = TaxPaymentStatus.None)
        {
            var partner = _db.Properties.Include(x => x.LegalEntity).ThenInclude(x => x.Partner).Where(x => x.Id == g.PropertyId).FirstOrDefault().LegalEntity.Partner;

            // Ako je nautička taksa u pitanju, onda je računamo na potpuno drugačiji način
            if (g.VesselId.HasValue)
            { 
                _db.Entry(g).Reference(a => a.Vessel).Load();
                var vessel = g.Vessel!;

                if(vessel.CountryId == 505) //plovila koja plove pod MNE zastavom ne placaju taksu
                {
                    g.ResTaxAmount = 0;
                }
                else
                {
                    var days = (int)(g.CheckOut!.Value - g.CheckIn!.Value).TotalDays;
                    var tax = _db.NauticalTax
                                .Where(a => vessel.Length >= a.LowerLimitLength && vessel.Length <= a.UpperLimitLength)
                                .Where(a => days >= a.LowerLimitPeriod && days <= a.UpperLimitPeriod)
                                .FirstOrDefault();

                    g.ResTaxAmount = tax?.Amount;
                } 
            }

            /*
            else
            {
                foreach (MnePerson p in g.Persons)
                {
                    if (p.CheckOut.HasValue == false) throw new Exception($"Gost {p.FirstName} {p.LastName} nema definisan datum odjave. Molimo unesite datum kako bi ste kompletirali prijavu.");

                    var zero = new DateTime(1, 1, 1);
                    var span = (DateTime.Now.Date - p.BirthDate.Date);
                    var age = (zero + span).Year - 1;

                    var resTaxType = _db.ResTaxTypes.Where(a => a.PartnerId == partner.Id).FirstOrDefault(a => a.AgeFrom <= age && age <= a.AgeTo);
                    if (resTaxType != null)
                    {
                        p.ResTaxTypeId = resTaxType.Id;

                        var days = (int)(p.CheckOut.Value.Date - p.CheckIn.Date).TotalDays;
                        if (days < 0) days = 0;
                        p.ResTaxAmount = resTaxType.Amount * days;

                        //var resPaymentType = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == partner.Id).FirstOrDefault(a => a.PaymentStatus == TaxPaymentStatus.Card);
                        //p.ResTaxPaymentTypeId = resPaymentType.Id;

                        //p.ResTaxFee = CalcResTaxFee(p.ResTaxAmount ?? 0, partner.Id, p.ResTaxPaymentTypeId ?? 0);

                        p.ResTaxFee = 0;

                        _db.SaveChanges();
                    }
                }
            }
            */

            // Pokušamo da nađemo način plaćanja grupe
            if (g.ResTaxPaymentTypeId == null && pay != TaxPaymentStatus.None)
            {
                var payId = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == partner.Id).FirstOrDefault(a => a.PaymentStatus == pay);
                g.ResTaxPaymentTypeId = payId?.Id;
            }

            // I onda računamo proviziju grupe
            if (g.VesselId == null)
            {
                var persons = _db.MnePersons.Where(a => a.GroupId == g.Id).ToList();
                g.ResTaxAmount = _db.MnePersons.Where(a => a.GroupId == g.Id).Select(a => a.ResTaxAmount).Sum();
                if (g.ResTaxPaymentTypeId != null)
                {
                    g.ResTaxFee = CalcResTaxFee(g.ResTaxAmount ?? 0, partner.Id, g.ResTaxPaymentTypeId ?? 0);
                    g.ResTaxCalculated = true;
                    g.ResTaxPaid = false;
                }
            }
            else
            {
                if (g.ResTaxPaymentTypeId != null)
                {
                    g.ResTaxFee = CalcResTaxFee(g.ResTaxAmount ?? 0, partner.Id, g.ResTaxPaymentTypeId ?? 0);
                    g.ResTaxCalculated = true;
                    g.ResTaxPaid = false;
                }
            }

            _db.SaveChanges();
        }

        public decimal CalcResTaxFee(decimal amount, int partnerId, int paymentTypeId)
        {
            if (paymentTypeId != 0)
            {
                var resTaxPayment = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == partnerId).Include(a => a.PaymentFees).FirstOrDefault(a => a.Id == paymentTypeId);
                if (resTaxPayment != null && resTaxPayment.PaymentFees.Any())
                {
                    var resTaxFee = resTaxPayment.PaymentFees.Where(a => a.LowerLimit <= amount && amount <= a.UpperLimit).FirstOrDefault();
                    if (resTaxFee != null)
                    {
                        if (resTaxFee.FeeAmount.HasValue) return resTaxFee.FeeAmount.Value;
                        if (resTaxFee.FeePercentage.HasValue) return resTaxFee.FeePercentage.Value / 100m * amount;
                    }
                }
            }
            return 0;
        }

        public override async Task<Person> PersonFromMrz(MrzDto mrz)
        {
            var mnePerson = new MnePerson();
            mnePerson.Guid = Guid.NewGuid().ToString();
            mnePerson.PropertyId = 0;
            mnePerson.CheckIn = DateTime.Now;
            mnePerson.LegalEntityId = this._legalEntity.Id;
            mnePerson.FirstName = mrz.HolderNamePrimary;
            mnePerson.LastName = mrz.HolderNameSecondary;
            mnePerson.Gender = mrz.HolderSex;
            mnePerson.BirthDate = mrz.HolderDateOfBirth == null ? DateTime.Now : DateTime.ParseExact(mrz.HolderDateOfBirth, "yyyyMMdd", null);
            mnePerson.Nationality = mrz.HolderNationality;
            mnePerson.DocumentCountry = mrz.DocIssuer;
            mnePerson.DocumentNumber = mrz.DocNumber;
            mnePerson.DocumentValidTo = DateTime.ParseExact(mrz.DocExpiry, "yyyyMMdd", null);
            mnePerson.DocumentIssuer = mrz.DocIssuer ?? "Nepoznato";
            mnePerson.DocumentType = "";
            mnePerson.BirthCountry = mrz.HolderNationality;
            mnePerson.PersonalNumber = mrz.HolderNumber ?? "Nepoznato";
            mnePerson.PermanentResidenceCountry = mrz.DocIssuer;
            mnePerson.PermanentResidenceAddress = "Nepoznato";
            mnePerson.PermanentResidencePlace = "Nepoznato";
            _db.MnePersons.Add(mnePerson);

            _db.SaveChanges();

            return mnePerson;
        }

        public override List<PersonErrorDto> Validate(Group group, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var result = new List<PersonErrorDto>();
            _db.Entry(group).Collection(a => a.Persons).Load();
            foreach (MnePerson p in group.Persons)
            {
                _db.Entry(p).Reference(a => a.LegalEntity).Load();
                var one = Validate(p, checkInDate, checkOutDate);
                if (one.ValidationErrors.Any()) result.Add(one);
            }
            return result;
        }

        public override PersonErrorDto Validate(Person person, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var p = person as MnePerson;
            var err = new PersonErrorDto();
            var now = DateTime.Now;

            if (p.DocumentValidTo < now) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentValidTo), Error = "Datum važenja ličnog dokumenta mora biti u budućnosti." });
            if (p.BirthDate.Date >= now) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthDate), Error = "Datum rođenja mora biti u prošlosti." });
            if (p.CheckIn.Date >= now) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.CheckIn), Error = "Datum prijave ne smije biti u budućnosti." });
            if (p.CheckOut.HasValue && p.CheckOut.Value.Date < p.CheckIn.Date) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.CheckIn), Error = "Datum odjave mora biti nakon datuma prijave." });
            if (p.ExternalId != null && p.CheckOut.HasValue && p.CheckOut.Value.Date < now.Date) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.ExternalId), Error = "Neaktivan boravak se ne može mijenjati." });
            if (string.IsNullOrEmpty(p.FirstName)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.FirstName), Error = "Morate unijeti ime gosta." });
            if (string.IsNullOrEmpty(p.LastName)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.LastName), Error = "Morate unijeti prezime gosta." });
            if (string.IsNullOrEmpty(p.Nationality)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.Nationality), Error = "Morate unijeti državljanstvi gosta." });
            if (string.IsNullOrEmpty(p.PersonalNumber)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.PersonalNumber), Error = "Morate unijeti matični broj gosta." });
            if (string.IsNullOrEmpty(p.Gender)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.Gender), Error = "Morate unijeti pol gosta." });
            if (string.IsNullOrEmpty(p.BirthPlace)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthPlace), Error = "Morate unijeti mjesto rođenja gosta." });
            if (p.BirthDate == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.BirthDate), Error = "Morate unijeti državu rođenja gosta." });
            if (string.IsNullOrEmpty(p.PermanentResidencePlace)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.PermanentResidencePlace), Error = "Morate unijeti mjesto prebivališta gosta." });
            if (string.IsNullOrEmpty(p.PermanentResidenceAddress)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.PermanentResidenceAddress), Error = "Morate unijeti adresu prebivališta gosta." });
            if (string.IsNullOrEmpty(p.PermanentResidenceCountry)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.PermanentResidenceCountry), Error = "Morate unijeti državu prebivališta gosta." });
            if (string.IsNullOrEmpty(p.DocumentType)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentType), Error = "Morate unijeti vrstu ličnog dokumenta gosta." });
            if (string.IsNullOrEmpty(p.DocumentNumber)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentNumber), Error = "Morate unijeti broj ličnog dokumenta gosta." });
            if (string.IsNullOrEmpty(p.DocumentCountry)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentCountry), Error = "Morate unijeti državu izdavanja ličnog dokumenta gosta." });
            if (string.IsNullOrEmpty(p.DocumentIssuer)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.DocumentIssuer), Error = "Morate unijeti izdavaoca ličnog dokumenta gosta." });

            if (_legalEntity.PartnerId == 4 && p.PersonType == "4")
            {
                if (string.IsNullOrEmpty(p.EntryPoint)) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.EntryPoint), Error = "Morate unijeti granični prelaz." });
                if (p.EntryPointDate == null) err.ValidationErrors.Add(new PersonValidationError() { Field = nameof(p.EntryPointDate), Error = "Morate unijeti datum ulaska u Crnu Goru." });
            }

            return err;
        }

        public async override Task<object> Properties(LegalEntity legalEntity)
        {
            (user user, smjestajniObjekat[] so, X509Certificate2 cert) = ((user, smjestajniObjekat[], X509Certificate2))await Authenticate(legalEntity);
            try
            {
                foreach (var s in so)
                {
                    if (_db.Properties.Any(a => a.LegalEntityId == legalEntity.Id && a.ExternalId == s.id) == false)
                    {
                        var property = new Property();
                        property.LegalEntityId = legalEntity.Id;
                        property.ExternalId = s.id;
                        property.Type = "";
                        property.Address = "";
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

        /*
        public async Task<Document> CreateInvoice(Group g, PaymentType? pay)
        { 
            var doc = new Document();
            doc.GroupId = g.Id;
            doc.PropertyId = g.PropertyId;
            doc.LegalEntityId = g.LegalEntityId;
            doc.InvoiceDate = DateTime.Now;
            doc.BusinessUnitCode = g.Property.BusinessUnitCode;
            doc.FiscalEnuCode = g.Property.FiscalEnuCode;
            doc.OperatorCode = _user.EfiOperator;
            var person = g.Persons.First() as MnePerson;

            if (person != null)
            {
                doc.PartnerName = $"{person.FirstName} {person.LastName}";
                doc.PartnerIdType = person.DocumentType switch { "1" => BuyerIdType.Passport, "2" => BuyerIdType.ID, _ => BuyerIdType.Passport };
                doc.PartnerIdNumber = person.DocumentNumber;
                doc.PartnerType = BuyerType.Person;
                doc.PartnerAddress = person.PermanentResidenceAddress ?? "";
            }

            _db.Documents.Add(doc);
            _db.SaveChanges();

            doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
            _db.SaveChanges();

            (var bor, var btax) = CheckItems();

            var boravak = g.Persons.Select(a => a as MnePerson).ToList().Select(a => new
            {
                Item = bor,
                Quant = Math.Round(((a.CheckOut ?? DateTime.Now) - a.CheckIn).TotalDays, 0),
                Price = a.Property.Price ?? 1m
            }).ToList();

            var taxes = g.Persons.Select(a => a as MnePerson).ToList().Select(a => new
            {
                Item = btax,
                Quant = Math.Round(((a.CheckOut ?? DateTime.Now) - a.CheckIn).TotalDays, 0),
                Price = (a.Property.ResidenceTax ?? 1) * (new DateTime(1, 1, 1) + (DateTime.Now - a.BirthDate)).Year - 1 switch { >= 18 => 1m, >= 12 => 0.5m, _ => 0m }
            }).ToList();

            foreach (var i in boravak.Union(taxes))
            {
                var sd = new DocumentItem();
                sd.DocumentId = doc.Id;
                sd.ItemId = i.Item.Id;
                sd.ItemCode = i.Item.Code ?? "";
                sd.ItemName = i.Item.Name;
                sd.ItemUnit = i.Item.Unit;
                sd.UnitPrice = i.Price;
                sd.Quantity = (decimal)i.Quant;
                sd.Discount = 0;
                sd.FinalPrice = i.Price;
                sd.VatExempt = i.Item.VatExempt;
                _db.DocumentItems.Add(sd);
                _db.SaveChanges();
                SetItemValues(sd);
                _db.SaveChanges();
            }

            doc.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum();
            _db.SaveChanges();

            Payment(doc, pay ?? PaymentType.Cash);

            return doc;
        }

        public (Item, Item) CheckItems()
        {
            var le = _user.LegalEntityId;

            var boravak = _db.Items.Where(a => a.LegalEntityId == le && a.Code == "BORAV").FirstOrDefault();                
            if (boravak == null)
            {
                boravak = new Item();
                boravak.LegalEntityId = le;
                boravak.Code = "BORAV";
                boravak.Name = "Usluga smještaja";
                boravak.Description = "Usluga smještaja";
                boravak.Unit = "KOM";
                boravak.VatRate = _user.LegalEntity.InVat ? 21 : 0; // Konfigurisati stopu poreza
                boravak.Price = 100;
                _db.Items.Add(boravak);
                _db.SaveChanges();
            }

            var btax = _db.Items.Where(a => a.LegalEntityId == le && a.Code == "BTAX").FirstOrDefault();            
            if (btax == null)
            {
                btax = new Item();
                btax.LegalEntityId = le;
                btax.Code = "BTAX";
                btax.Name = "Boravišna taksa";
                btax.Description = "Boravišna taksa";
                btax.Unit = "KOM";
                btax.VatRate = 0;
                btax.VatExempt = MneVatExempt.VAT_CL20;
                btax.Price = 1;
                _db.Items.Add(btax);
                _db.SaveChanges();
            }

            return (boravak, btax);
        }

        public void SetItemValues(DocumentItem s)
        {
            s.ItemUnit = s.Item.Unit;
            s.UnitPriceWoVat = s.UnitPrice * 100m / (100m + s.VatRate);
            s.LineAmount = s.Quantity * s.UnitPrice;
            s.LineTotalWoVat = s.Quantity * s.FinalPrice * 100m / (100m + s.VatRate);
            s.VatAmount = s.Quantity * s.FinalPrice * s.VatRate / (100m + s.VatRate);
            s.LineTotal = s.Quantity * s.FinalPrice;
        }

        public void Payment(Document doc, PaymentType pType)
        {
            var payment = doc.DocumentPayments.FirstOrDefault();
            if (payment == null)
            {
                payment = new DocumentPayment();
                payment.DocumentId = doc.Id;
                payment.PaymentType = pType;
                payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
                _db.DocumentPayments.Add(payment);
                _db.SaveChanges();
            }
            else
            {
                payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
                payment.PaymentType = pType;
                _db.SaveChanges();
            }
        }
        */

        public async Task<Stream> InvoicePdf(Document doc, string output = "pdf")
        {
            var docxProvider = new DocxFormatProvider();
            var pdfProvider = new PdfFormatProvider();
            RadFlowDocument document;

            //var path = _webHostEnvironment.WebRootPath + _configuration["REPORINT:MNE:Invoice"];
            var path = "C:\\CERT\\MneInvoice.docx";

            using (Stream input = File.OpenRead(path))
            {
                document = docxProvider.Import(input);
            }

            RadFlowDocumentEditor editor = new RadFlowDocumentEditor(document);

            IEnumerable<Table> tables = document.EnumerateChildrenOfType<Table>();

            var header = tables.FirstOrDefault();
            var invoice = tables.Skip(1).FirstOrDefault();
            var items = tables.Skip(2).FirstOrDefault();
            var totals = tables.Skip(3).FirstOrDefault();
            var taxes = tables.Skip(4).FirstOrDefault();

            using (Stream firstImage = File.OpenRead(@"C:\CERT\26.png"))
            {
                //var inImage = header.Rows[0].Cells[2].Blocks.AddParagraph().Inlines.AddImageInline();
                header.Rows[0].Cells[2].Blocks.Clear();
                var inImage = header.Rows[0].Cells[2].Blocks.AddParagraph().Inlines.AddImageInline();
                inImage.Image.ImageSource = new Telerik.Windows.Documents.Media.ImageSource(firstImage, "png");
                inImage.Image.SetWidth(true, 110);
                inImage.Paragraph.TextAlignment = Telerik.Windows.Documents.Flow.Model.Styles.Alignment.Right;
            }

            header.Rows.First().Cells.Skip(1).First().ColumnSpan = 2;

            var c = 1;
            foreach (var i in doc.DocumentItems)
            {
                var row = items.Rows.AddTableRow();
                row.Height = new TableRowHeight(HeightType.Exact, 30);

                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{c++}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.ItemName}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.ItemUnit}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.Quantity}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.UnitPriceWoVat}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.Discount}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.FinalPrice}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.VatRate}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.FinalPrice}");
                row.Cells.AddTableCell().Blocks.AddParagraph().Inlines.AddRun($"{i.LineAmount.ToString("n2")}");

                foreach (var cell in row.Cells)
                {
                    //cell.VerticalAlignment = VerticalAlignment.Center;                    
                    cell.Padding = new Telerik.Windows.Documents.Primitives.Padding(5);
                    cell.Blocks.OfType<Paragraph>().First().Spacing.SpacingAfter = 5;
                    cell.Blocks.OfType<Paragraph>().First().Spacing.SpacingBefore = 3;

                    if (new int[] { 1 }.Contains(cell.GridColumnIndex)) cell.Blocks.OfType<Paragraph>().First().TextAlignment = Alignment.Left;
                    else if (new int[] { 0, 2 }.Contains(cell.GridColumnIndex)) cell.Blocks.OfType<Paragraph>().First().TextAlignment = Alignment.Center;
                    else cell.Blocks.OfType<Paragraph>().First().TextAlignment = Alignment.Right;

                    if (cell.GridRowIndex % 2 == 1) cell.Shading.BackgroundColor = ThemableColor.FromArgb(255, 255, 255, 255);
                    else cell.Shading.BackgroundColor = ThemableColor.FromArgb(255, 225, 225, 225);
                }
            }

            c = 2;
            foreach (var t in doc.DocumentItems.GroupBy(a => new { a.VatRate, a.VatExempt }))
            {
                var row = totals.Rows.Skip(c++).FirstOrDefault();
                //row.Height = new TableRowHeight(HeightType.Exact, 10);

                row.Cells.Skip(0).First().Blocks.OfType<Paragraph>().First().Inlines.AddRun($"{(t.Key.VatExempt != null ? $"Oslob. po čl. {t.Key.VatExempt.ToString().Replace("VAT_CL", "")}" : $"{t.Key.VatRate}%")}");
                row.Cells.Skip(1).First().Blocks.OfType<Paragraph>().First().Inlines.AddRun($"{t.Sum(a => a.LineTotalWoVat)}");
                row.Cells.Skip(2).First().Blocks.OfType<Paragraph>().First().Inlines.AddRun($"{t.Sum(a => a.VatAmount * a.Quantity).ToString("n2")}");

                foreach (var cell in row.Cells.Take(3))
                {
                    cell.Padding = new Telerik.Windows.Documents.Primitives.Padding(4);
                    cell.Blocks.OfType<Paragraph>().First().Spacing.SpacingAfter = 2;
                    cell.Blocks.OfType<Paragraph>().First().Spacing.SpacingBefore = 0;
                    cell.Blocks.OfType<Paragraph>().First().TextAlignment = Alignment.Right;
                    cell.Borders = new TableCellBorders(new Border(BorderStyle.Thick));
                    //if (cell.GridRowIndex % 2 == 1) cell.Shading.BackgroundColor = ThemableColor.FromArgb(255, 255, 255, 255);
                    //else cell.Shading.BackgroundColor = ThemableColor.FromArgb(255, 225, 225, 225);
                }
            }

            var stream = new MemoryStream();
            if (output == "docx") docxProvider.Export(document, stream);
            if (output == "pdf") pdfProvider.Export(document, stream);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public override Task GuestListMail(int objekat, string datumOo, string datumdo, string email)
        {
            throw new NotImplementedException();
        }

        public override async Task<Stream> GuestListPdf(int objekat, string datumod, string datumdo, int? partnerId)
        {
            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var reportName = _configuration["REPORTING:MNE:GuestList"];
            var report = _db.Reports.FirstOrDefault(a => a.Name == reportName);
            var path = Path.Combine(_env.ContentRootPath, "Reports", report.Path);

            var dateFrom = DateTime.ParseExact(datumod, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            var dateTo = DateTime.ParseExact(datumdo, "dd.MM.yyyy", CultureInfo.InvariantCulture);

            reportSource.Uri = path;
            reportSource.Parameters.Add("Property", objekat);
            reportSource.Parameters.Add("DateFrom", dateFrom);
            reportSource.Parameters.Add("DateTo", dateTo);
            reportSource.Parameters.Add("LogoUrl", ReportHelpers.GetPartnerLogoUrl(_configuration, partnerId));

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

            if (!result.HasErrors)
            {
                return new MemoryStream(result.DocumentBytes);
            }
            else
            {
                return null;
            }
        }

        
    }
}
