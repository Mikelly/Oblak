using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Oblak.Data;
using Microsoft.AspNetCore.Identity;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Models.rb90;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Oblak.Helpers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Oblak.Services.SRB;
using Oblak.Services.SRB.Models;
using Oblak.Models.Api;
using SendGrid.Helpers.Mail;
using Oblak.Interfaces;
using System.Net;
using Oblak.Data.Enums;
using RB90;
using Oblak.Filters;

namespace Oblak.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : Controller
    {        
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApiController> _logger;        
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;
        private readonly eMailService _eMailService;
        private readonly DocumentService _documentService;
        private readonly MneClient _rb90Client;
        private readonly SrbClient _srbClient;        
        private readonly EfiClient _efiClient;
        private readonly IMapper _mapper;
        private readonly Register _registerClient;
        private ApplicationUser _appUser;        

        public ApiController(   
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ApiController> logger,            
            ApplicationDbContext db,
            IWebHostEnvironment env,
            eMailService eMailService,
            MneClient rb90Client,
            SrbClient srbClient,            
            EfiClient efiClient,
            DocumentService documentService,
            IMapper mapper
            )
        {             
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;            
            this.db = db;
            _env = env;
            _eMailService = eMailService;
            _rb90Client = rb90Client;
            _srbClient = srbClient;            
            _mapper = mapper;
            _efiClient = efiClient;
            _documentService = documentService;
            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
                if (_appUser != null)
                {
                    if (_appUser.LegalEntity.Country == Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                    if (_appUser.LegalEntity.Country == Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
                }
            }
        }

        [LoginResultFilter]
        [HttpPost]
        [Route("logout")]
        public async Task<ActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            var cookie = Request.Cookies[".AspNetCore.Identity.Application"];

            return Ok(new LoginDto() { info = "OK", error = "", auth = cookie, sess = "", oper = "", lang = "", cntr = "" });
        }

        [LoginResultFilter]
        [HttpPost]
        [Route("login")]
        public async Task<ActionResult> Login(string username, string password)
        {
            var user = db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            if (user != null)
            {
                var checkPassword = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

                if (checkPassword.Succeeded)
                {
                    var u = await _userManager.FindByNameAsync(username);
                    await _signInManager.SignInAsync(user, true);
                }
                else
                {
                    return Ok(new LoginDto() { info = "", error = "Neispravan username i/ili lozinka.", auth = "", sess = "", oper = "", lang = user.LegalEntity.Country.ToString(), cntr = user.LegalEntity.Country.ToString() });
                }                

                if (checkPassword.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");                    
                    return Ok(new LoginDto() { info = "", error = "User je zaključan.", auth = "", sess = "", oper = "", lang = user.LegalEntity.Country.ToString(), cntr = user.LegalEntity.Country.ToString() });
                }

                //return RedirectToAction("AfterLogin");

                var cookie = Request.Cookies[".AspNetCore.Identity.Application"];

                return Ok(new LoginDto() { info = "OK", error = "", auth = cookie, sess = "", oper = "", lang = user.LegalEntity.Country.ToString(), cntr = user.LegalEntity.Country.ToString() });
            }

            return Ok(new LoginDto() { info = "", error = "", auth = "", sess = "", oper = "", lang = "", cntr = "" });
        }

        [HttpPost]
        public async Task<LoginDto> AfterLogin(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            var cookie = Request.Cookies[".AspNetCore.Identity.Application"];

            return new LoginDto() { info = "OK", error = "", auth = cookie, sess = "", oper = "", lang = user.LegalEntity.Country.ToString(), cntr = user.LegalEntity.Country.ToString() };
        }

        [HttpPost]
        [Route("checkAuth")]
        public ActionResult CheckAuth()
        {
            return Json(HttpContext.User.Identity.IsAuthenticated);
        }

        /*
        [HttpGet]
        [Route("userInfo")]
        public async Task<ActionResult> UserInfo()
        {            
            var now = DateTime.Now.Date;            

            var email = _appUser.Email;
            var username = _appUser.UserName.ToLower();
            var active = true;
            //var expires = bz.ParameterData.Where(a => a.Parameter == "DEACT_ACC" && a.Entity == username && a.DateValue > now).OrderByDescending(a => a.DateValue).Select(a => a.DateValue).FirstOrDefault();

            return Ok(new { Data = new { username, email, active, DateTime.Now } });            
        }

        /*
        public async Task<ActionResult> Activate()
        {            
            var now = DateTime.Now.Date;
            var email = _appUser.Email;
            var username = _appUser.UserName.ToLower();
            var active = true;

            return Ok(new { Data = new { username, email, active, DateTime.Now } });            
        }


        public async Task<ActionResult> Deactivate()
        {            
            var now = DateTime.Now.Date;
            var expires = new DateTime(now.Year, now.Month, 1).AddMonths(1);

            var user = await _userManager.GetUserAsync(HttpContext.User);

            var email = user.Email;
            var username = user.UserName.ToLower();
            var active = true;            

            return Ok(new { Data = new { username, email, active, DateTime.Now } });
        }


        public ActionResult CheckAuth()
        {
            if (User.Identity.IsAuthenticated) return Content("1");
            else return Content("0");
        }
        */

        [HttpGet]
        [Route("propertiesGet")]
        public async Task<ActionResult<List<PropertyDto>>> PropertiesGet()
        {      
            var data = db.Properties.Where(a => a.LegalEntityId == _appUser!.LegalEntityId).ToList()
                .Select(a => _mapper.Map<Property, PropertyDto>(a)).ToList();
            
            return Json(data);
        }

        [HttpGet]
        [Route("propertiesExternal")]
        public async Task<ActionResult<List<PropertyDto>>> PropertiesExternal()
        {
            var result = await _registerClient.Properties();

            var data = db.Properties.Where(a => a.LegalEntityId == _appUser!.LegalEntityId).ToList()
                .Select(a => _mapper.Map<Property, PropertyDto>(a)).ToList();

            return Json(data);
        }

        [HttpGet]
        [Route("units")]
        public async Task<ActionResult> Units()
        {            
            return Json(db.Properties.Where(a => a.LegalEntityId == _appUser!.LegalEntityId));            
        }

        [HttpGet]
        [Route("items")]
        public async Task<ActionResult> Itmes()
        {            
            return Json(db.Items.Where(a => a.LegalEntityId == _appUser.LegalEntityId).Select(a => new { ID = a.Id, Naziv = a.Name, JedinicaMjere = a.Unit, Sifra = a.Code, Porez = a.VatRate, Cijena = a.Price }));
        }

        [HttpGet]
        [Route("CodeLists")]
        public async Task<ActionResult<List<CodeListDto>>> CodeLists()
        {
            return Json(await _registerClient.CodeLists());            
        }

        [HttpGet]
        [Route("groupList")]
        public async Task<ActionResult<List<GroupEnrichedDto>>> Groups(int page = 1)
        {
            var user = _appUser;

            var grupe = db.Groups.Where(a => a.LegalEntityId == user.LegalEntityId).OrderByDescending(a => a.Date).Skip((page - 1) * 50).Take(50);//.Select(a => new { a.Id, a.Date, a.PropertyId, a.UnitId, BrojGostiju = db.rb90Persons.Where(b => b.GroupId == a.Id).Count(), Gosti = db.rb90GuestList(a.Id), a.Status }).ToList();

            var data = grupe.Select(a => new GroupEnrichedDto {
                Id = a.Id,
                Date = a.Date,
                Status = a.Status,
                PropertyId = a.PropertyId,
                UnitId = a.UnitId,
                GUID = a.Guid,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                Email = a.Email,                
                LegalEntityId = a.LegalEntityId,
                PropertyName = a.Property.Name,                
                Guests = db.GuestList(a.Id),
                NoOfGuests = a.Persons.Count(),
            });

            return Json(data);            
        }

        [HttpPost]
        [Route("groupCreate")]
        public async Task<ActionResult<GroupDto>> GroupCreate(GroupDto groupDto)
        {
            var group = await _registerClient.Group(groupDto);

            return Json(_mapper.Map(group, groupDto));
        }


        [HttpGet]
        [Route("groupGet")]
        public async Task<ActionResult<GroupEnrichedDto>> GroupGet(int id)
        {
            var m = db.Groups.SingleOrDefault(a => a.Id == id);

            return _mapper.Map<GroupEnrichedDto>(m);
        }


        [HttpGet]
        [Route("groupDelete")]
        public ActionResult<BasicDto> GroupDelete(int id)
        {
            var group = db.Groups.SingleOrDefault(a => a.Id == id)!;

            if (group.Persons.Any())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new BasicDto() { error = "Ne možete obrisati prijavu dok ne obrišete sve goste iz nje.", info = "" };
            }
            else
            {
                db.Groups.Remove(group);
                db.SaveChanges();
                return new BasicDto() { error = "", info = "Uspješno obrisana prijava" };
            }
        }

        [HttpGet]
        [Route("groupValidate")]
        public List<PersonErrorDto> Check(int id, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var group = db.Groups.Include(a => a.LegalEntity).Where(a => a.Id == id).First();
            var legalEntity = group.LegalEntity;
            var errors = _registerClient.Validate(group, checkInDate, checkOutDate); 
            return errors;
        }

        [HttpGet]
        [Route("groupRegister")]
        public async Task<ActionResult> GroupRegister(int id, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var group = db.Groups.Include(a => a.LegalEntity).Where(a => a.Id == id).First();
            var legalEntity = group.LegalEntity;

            try
            {
                var result = await _registerClient.RegisterGroup(group, checkInDate, checkOutDate);
                if (result != null) Response.StatusCode = 400;
                return Json(result);
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(Exceptions.StringException(e));
            }
        }


        [HttpGet]
        [Route("personMneList")]
        public ActionResult<List<MnePersonDto>> PersonMneList(int id)
        {
            return Json(_mapper.Map<List<MnePersonDto>>(db.MnePersons.Where(a => a.GroupId == id).ToList()));            
        }


        [HttpGet]
        [Route("personSrbList")]
        public ActionResult<List<SrbPersonDto>> PersonSrb0List(int id)
        {
            return Json(_mapper.Map<List<SrbPersonDto>>(db.SrbPersons.Where(a => a.GroupId == id).ToList()));
        }


        [HttpPost]
        [Route("personMneSave")]
        public ActionResult<MnePersonDto> PersonMne(MnePersonDto gost)
        {
            _logger.LogDebug("START GOST");

            var g = db.Groups.Where(a => a.Id == gost.GroupId).FirstOrDefault();
            var o = db.Properties.Where(a => a.Id == g.PropertyId).FirstOrDefault();
            MnePerson m = null;

            _logger.LogDebug("AFTER START GOST");

            if (gost.Id == 0)
            {
                _logger.LogDebug("NOVI GOST");
                try
                {
                    m = new MnePerson();
                    _mapper.Map(gost, m);                    
                    m.PropertyId = o.Id;
                    m.PropertyExternalId = o.ExternalId;
                    m.LegalEntityId = _appUser.LegalEntity.Id;
                    db.MnePersons.Add(m);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogDebug(Exceptions.StringException(e));
                }
            }
            else
            {
                _logger.LogDebug("STARI GOST");
                try
                {
                    m = db.MnePersons.SingleOrDefault(a => a.Id == gost.Id)!;
                    _mapper.Map(gost, m);                    
                    m.PropertyId = o.Id;
                    m.PropertyExternalId = o.ExternalId;
                    m.LegalEntityId = _appUser.LegalEntity.Id;
                    db.MnePersons.Add(m);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogDebug(Exceptions.StringException(e));
                }
            }

            return Json(_mapper.Map<MnePersonDto>(m));
        }


        [HttpPost]
        [Route("personSrbSave")]
        public async Task<ActionResult<SrbPersonDto>> PersonSrb(SrbPersonDto gost)
        {
            _logger.LogDebug("START GOST");

            var g = db.Groups.Where(a => a.Id == gost.GroupId).FirstOrDefault();
            var o = db.Properties.Where(a => a.Id == g.PropertyId).FirstOrDefault();
            MnePerson m = null;

            var result = await _registerClient.Person(gost);

            return Json(_mapper.Map<SrbPersonDto>(result));
            /*
            _logger.LogDebug("AFTER START GOST");

            if (gost.Id == 0)
            {
                _logger.LogDebug("NOVI GOST");
                try
                {
                    m = new MnePerson();
                    _mapper.Map(gost, m);
                    m.PropertyAddress = "";
                    m.PropertyName = "";
                    m.PropertyNumber = "";
                    m.PropertyId = o.Id;
                    m.PropertyExternalId = o.ExternalId;
                    m.LegalEntityId = _appUser.LegalEntity.Id;
                    db.MnePersons.Add(m);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogDebug(Exceptions.StringException(e));
                }
            }
            else
            {
                _logger.LogDebug("STARI GOST");
                try
                {
                    m = db.MnePersons.SingleOrDefault(a => a.Id == gost.Id)!;
                    _mapper.Map(gost, m);
                    m.PropertyAddress = "";
                    m.PropertyName = "";
                    m.PropertyNumber = "";
                    m.PropertyId = o.Id;
                    m.PropertyExternalId = o.ExternalId;
                    m.LegalEntityId = _appUser.LegalEntity.Id;
                    db.MnePersons.Add(m);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.LogDebug(Exceptions.StringException(e));
                }
            }

            return Json(_mapper.Map<SrbPersonDto>(m));
            */
        }


        [HttpGet]
        [Route("certificateMail")]
        public async Task<ActionResult<BasicDto>> CertificateMail(int id, string email)
        {
            if (email == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nije unesena e-mail adresa!" });
            }
            var group = db.Groups.FirstOrDefault(a => a.Id == id);
            await _registerClient.CertificateMail(group, email);

            return Json(new { info = "Uspješno poslate potvrde putem e-maila!", error = "" });
        }


        [HttpGet]
        [Route("certificatePdf")]
        public async Task<ActionResult> CertificatePdf(int id)
        {
            var grp = db.Groups.Include(a => a.Persons).Where(a => a.Id == id).FirstOrDefault();

            if (grp == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nepostojeća prijava gostiju!" });
            }

            try
            {                
                var stream = await _registerClient.CertificatePdf(grp);
                stream.Seek(0, SeekOrigin.Begin);
                var fsr = new FileStreamResult(stream, "application/pdf");
                fsr.FileDownloadName = $"Potvrde.pdf";
                return fsr;
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }



        #region Racun


        [HttpPost]
        [Route("invoiceCreate")]
        public async Task<ActionResult> InvoiceCreate(int? property, int? group, Invoice? racun, PaymentType? pay)
        {
            try
            {
                _logger.LogDebug("RACUN START:");
                Property p;
                if(property.HasValue) p = db.Properties.FirstOrDefault(a => a.Id == property)!;
                else p = db.Properties.Where(a => a.LegalEntityId == _appUser.LegalEntityId).FirstOrDefault()!;

                if (group.HasValue && _registerClient is MneClient)
                {
                    var g = db.Groups.Include(a => a.Persons).FirstOrDefault(a => a.Id == group);
                    var rac = (_registerClient as MneClient).CreateInvoice(g, pay);
                    return Json(rac);
                }
                else
                {
                    var rac = _documentService.CreateRacun(racun, p);
                    return Json(rac);
                }
                
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                _logger.LogDebug("RACUN ERROR: " + Exceptions.StringException(e));
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }

        [HttpGet]
        [Route("invoiceGet")]
        public ActionResult InvoiceGet(int id)
        {
            Document doc = db.Documents.Where(a => a.Id == id).FirstOrDefault();

            var rac = _documentService.Doc2Racun(doc);

            return Json(rac);
        }


        [HttpGet]
        [Route("invoiceList")]
        public ActionResult InvoiceList(int page = 1, string status = "A")
        {
            var docStatus = status switch
            {
                "A" => DocumentStatus.Active,
                "F" => DocumentStatus.Fiscalized,
                "N" => DocumentStatus.NotFiscalized,
                "P" => DocumentStatus.Posted,
                _ => DocumentStatus.None
            };

            var data = db.Documents.Where(a => a.LegalEntityId == _appUser.LegalEntityId)
                .Where(a => a.Status == docStatus || status == null || docStatus == DocumentStatus.None)
                .OrderByDescending(a => a.InvoiceDate).Skip((page - 1) * 50).Take(50).ToList();

            var result = new List<Invoice>();

            foreach (var d in data) result.Add(_documentService.Doc2Racun(d));

            return Json (result);
        }

        [HttpGet]
        [Route("invoiceFiscal")]
        public async Task<ActionResult> InvoiceFiscal(int id)
        {
            var doc = db.Documents.Include(a => a.DocumentItems).Include(a => a.DocumentPayments).Where(a => a.Id == id).FirstOrDefault();

            if (doc.Status == DocumentStatus.Fiscalized)
            {
                Response.StatusCode = 400;
                return Json(new { info = "Račun je već fiskalizovan", error = "", id = doc.Id });
            }

            try
            {
                await _efiClient.Fiscalize(doc, null, null);                
                return Json(new { info = "Uspješno izvršena fiskalizacija", error = "", id = doc.Id });
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                _logger.LogDebug("FISKAL ERROR: " + Exceptions.StringException(e));
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }


        [HttpGet]
        [Route("invoiceDelete")]
        public ActionResult InvoiceDelete(int racun)
        {
            var doc = db.Documents.Where(a => a.Id == racun).FirstOrDefault();

            if (doc.Status == DocumentStatus.Fiscalized || doc.Status == DocumentStatus.NotFiscalized) return Json(new { error = "Račun je fiskalizovan ili poslat na fiskalizaciju, pa se ne može brisati", info = "", id = doc.Id });

            try
            {
                _documentService.DeleteRacun(doc);
                return Json(new { info = "Uspješno obrisan dokument", error = "", id = doc.Id });
            }
            catch (Exception e)
            {
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }


        [HttpGet]
        [Route("invoiceStorno")]
        public async Task<ActionResult> InvoiceStorno(int id)
        {
            var doc = db.Documents.Where(a => a.Id == id).FirstOrDefault();

            if (doc!.Status != DocumentStatus.Fiscalized)
            {
                Response.StatusCode = 400;
                return Json(new { error = "Račun nije fiskalizovan pa se ne može stornirati", info = "", id = doc.Id });
            }

            if (db.Documents.Any(a => a.DocumentId == doc.Id))
            {
                return Json(new { error = "Račun je već storniran", info = "", id = doc.Id });
            }

            try
            {                
                var storno = _documentService.Storno(doc);
                await _efiClient.Fiscalize(storno, doc.IIC, null);
                return Json(new { info = "Uspješno storniran dokument", error = "", id = doc.Id });
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }

        [HttpGet]
        [Route("invoiceMail")]
        public async Task<ActionResult> InvoiceMail(int id, string email)
        {
            var doc = db.Documents.Where(a => a.Id == id).FirstOrDefault();

            if (doc.Status != DocumentStatus.Fiscalized)
            {
                Response.StatusCode = 400;
                return Json(new { error = "Račun nije fiskalizovan pa se ne može slati na mail", info = "", id = doc.Id });
            }

            EmailAddressAttribute em = new EmailAddressAttribute();
            if (em.IsValid(email) == false)
            {
                Response.StatusCode = 400;
                return Json(new { error = "E-mail adresa nije ispravna", info = "", id = doc.Id });
            }

            try
            {
                await _documentService.InvoiceEmail(doc.Id, email);
                return Json(new { info = "Uspješno poslat račun", error = "", id = doc.Id });
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }

        [HttpGet]
        [Route("invoicePdf")]
        public async Task<ActionResult> InvoicePdf(int id, string output)
        {
            var doc = db.Documents.Include(a => a.DocumentItems).ThenInclude(a => a.Item).Include(a => a.DocumentPayments).Where(a => a.Id == id).FirstOrDefault();

            if (doc.Status != DocumentStatus.Fiscalized)
            {
                Response.StatusCode = 400;
                return Json(new { error = "Račun nije fiskalizovan pa se ne može slati na mail", info = "", id = doc.Id });
            }

            try
            {
                var stream = await (_registerClient as MneClient).InvoicePdf(doc, output);
                stream.Seek(0, SeekOrigin.Begin);
                var fsr = new FileStreamResult(stream, $"{(output == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document")}");
                fsr.FileDownloadName = $"Faktura br. {doc.No.ToString("0")}.{output}";
                return fsr;
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }


        #endregion
               



        #region Boravisna Taksa

        [HttpPost]
        [Route("resTaxCalc")]
        public ActionResult<ResTax> ResTaxCalc(int objekat, string datumod, string datumdo)
        {
            var obj = db.Properties.FirstOrDefault(a => a.Id == objekat);            
                        
            var OD = DateTime.ParseExact(datumod, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var DO = DateTime.ParseExact(datumdo, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

            var tax = new ResTax();
            tax.Status = "A";
            tax.PropertyId = objekat;
            tax.LegalEntityId = obj.LegalEntityId;
            tax.Date = DateTime.Now;
            tax.DateFrom = OD;
            tax.DateTo = DO;
            db.ResTaxes.Add(tax);
            db.SaveChanges();

            var rb90Client = _registerClient as MneClient;

            rb90Client.CalcResTax(tax, objekat, OD, DO, "FULL", "STRANI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "FULL", "DOMACI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "HALF", "STRANI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "HALF", "DOMACI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "NONE", "STRANI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "NONE", "DOMACI");

            tax.Amount = tax.Items.Select(a => a.TotalTax).Sum();
            db.SaveChanges();
            
            return Json(tax);
        }

        [HttpGet]
        [Route("resTaxList")]
        public ActionResult<List<ResTax>> ResTaxList(int page = 1)
        {
            var data = db.ResTaxes
                .Where(a => a.LegalEntityId == _appUser.LegalEntityId)
                .Include(a => a.Items)
                .OrderByDescending(a => a.Date).Skip((page - 1) * 50).Take(50).ToList();

            return Json(data);
        }

        [HttpPost]
        [Route("resTaxDelete")]
        public ActionResult<BasicDto> ResTaxDelete(int id)
        {
            var tax = db.ResTaxes.Where(a => a.Id == id).FirstOrDefault();

            if (tax == null) return Json(new BasicDto { info = "", error = "Boravišna taksa ne postoji!" });

            if (tax.Status == "P") return Json(new BasicDto { info = "", error = "Boravišna taksa je plaćena pa se ne može brisati!" });

            db.Remove(tax);
            db.SaveChanges();

            return Json(new BasicDto { info = "Boravišna taksa je uspješno obrisana!", error = "" });
        }


        [HttpGet]
        [Route("resTaxPdf")]
        public async Task<ActionResult> ResTaxPdf(int id)
        {
            var tax = db.ResTaxes.Where(a => a.Id == id).FirstOrDefault();

            if (tax == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nepostojeća boravišna taksa!" });
            }

            var obj = db.Properties.Where(a => a.Id == tax.PropertyId).FirstOrDefault();

            try
            {
                var stream = await _rb90Client.ResTaxPdf(id);

                var fsr = new FileStreamResult(stream, "application/pdf");
                fsr.FileDownloadName = $"Boravišna taksa za period od {tax.DateFrom.ToString("dd.MM.yyyy")} od {tax.DateTo.ToString("dd.MM.yyyy")} za smještajni objekat {obj!.Name}.pdf";
                return fsr;
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }


        [HttpGet]
        [Route("resTaxMail")]
        public async Task<ActionResult> ResTaxMail(int id, string email)
        {            
            var tax = db.ResTaxes.Where(a => a.Id == id).FirstOrDefault();

            if (tax == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nepostojeća boravišna taksa!" });
            }

            if (email == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nije unesena e-mail adresa!" });
            }

            var obj = db.Properties.Where(a => a.Id == tax.PropertyId).FirstOrDefault();

            var stream = _rb90Client.ResTaxPdf(id);

            var frm = _appUser.LegalEntity.Name;

            await _rb90Client.ResTaxEmail(id, _appUser.Email, email);

            return Json(new { info = "Uspješno poslata prijava boravišne takse putem maila!", error = "" });
        }

        #endregion


        [HttpPost]
        [Route("reports")]
        public ActionResult Reports(string id, string datum, string datumdo)
        {

            var report = id;
            var start = DateTime.Now.Date;
            var end = DateTime.Now.Date.AddDays(1);
            if (report == "X")
            {
                start = DateTime.Now.Date;
                end = DateTime.Now;
            }
            else if (report == "Z")
            {
                if (datum != null) start = DateTime.ParseExact(datum, "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
                else start = DateTime.Now.Date;

                end = start.AddDays(1).AddSeconds(-1);
            }
            else if (report == "Y" || report == "S" || report == "P" || report == "A" || report == "SLD")
            {
                start = DateTime.ParseExact(datum, "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
                end = DateTime.ParseExact(datumdo, "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
                end = end.AddDays(1).AddMinutes(-1);
            }

            return Ok();
        }
    }
}