using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Oblak.Data;
using Microsoft.AspNetCore.Identity;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Models.EFI;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Oblak.Helpers;
using System.ComponentModel.DataAnnotations;
using Oblak.Services.SRB;
using Oblak.Models.Api;
using Oblak.Data.Enums;
using Oblak.Filters;
using Oblak.Services.FCM;
using Oblak.Services.Payten;
using Oblak.Services.Reporting;
using Oblak.Services.Payment;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using SkiaSharp;
using static SQLite.SQLite3;

namespace Oblak.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : Controller
    {
        private const string _currencyCode = "EUR";

        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ApiController> _logger;        
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;
        private readonly eMailService _eMailService;
        private readonly DocumentService _documentService;   
        private readonly EfiClient _efiClient;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly Register _registerClient;
        private readonly LegalEntity _legalEntity;
        private ApplicationUser _appUser;   
        private readonly FcmService _fcmService;
        private readonly ApiService _paytenService;
        private readonly ReportingService _reporting;
        private readonly IConfiguration _configuration;
        private readonly PaymentService _paymentService;
        private readonly GroupService _groupService;

        public ApiController(   
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ApiController> logger,            
            ApplicationDbContext db,
            IWebHostEnvironment env,
            IConfiguration config,
            eMailService eMailService,
            MneClient rb90Client,
            SrbClient srbClient,            
            EfiClient efiClient,
            DocumentService documentService,
            FcmService fcmService,
            IMapper mapper,
            ApiService paytenService, 
            ReportingService reporting,
            PaymentService paymentService,
            IConfiguration configuration,
            GroupService groupService
            )
        {             
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;            
            this.db = db;
            _env = env;
            _eMailService = eMailService;
            _config = config;       
            _mapper = mapper;
            _efiClient = efiClient;
            _documentService = documentService;
            _fcmService = fcmService;
            _paytenService = paytenService;
            _reporting = reporting;
            _configuration = configuration;
            _paymentService = paymentService;
            _groupService = groupService;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).AsNoTracking().FirstOrDefault(a => a.UserName == username)!;
                if (_appUser != null)
                {
                    _legalEntity = _appUser.LegalEntity;
                    if (_appUser.LegalEntity.Country == Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>()!;
                    if (_appUser.LegalEntity.Country == Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>()!;
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
            var roles = new List<string>();
            if (user != null)
            {
                var checkPassword = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

                if (checkPassword.Succeeded)
                {
                    var u = await _userManager.FindByNameAsync(username);
                    roles = (await _userManager.GetRolesAsync(u)).ToList();
                    await _signInManager.SignInAsync(user, true);
                    _logger.LogInformation($"User Logged In: {username}");
                }
                else
                {                    
                    return Ok(new LoginDto() { info = "", error = "Neispravan username i/ili lozinka.", auth = "", sess = "", oper = "", lang = user.LegalEntity.Country.ToString(), cntr = user.LegalEntity.Country.ToString(), paym = false, test = false, prtn = null, roles = roles });
                }                

                if (checkPassword.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");                    
                    return Ok(new LoginDto() { info = "", error = "User je zaključan.", auth = "", sess = "", oper = "", lang = user.LegalEntity.Country.ToString(), cntr = user.LegalEntity.Country.ToString(), paym = false, test = false, prtn = null, roles = roles });
                }

                //return RedirectToAction("AfterLogin");

                var cookie = Request.Cookies[".AspNetCore.Identity.Application"];

                var py = false;
                if (user.LegalEntity.PartnerId.HasValue)
                { 
                    py = db.Partners.FirstOrDefault(a => a.Id == user.LegalEntity.PartnerId).ResidenceTaxPaymentRequired;
                }

                return Ok(new LoginDto() { info = "OK", error = "", auth = cookie, sess = "", oper = "", lang = user.LegalEntity.Country.ToString(), cntr = user.LegalEntity.Country.ToString(), paym = user.LegalEntity.PartnerId.HasValue ? user.LegalEntity.Partner.ResidenceTaxPaymentRequired : false, test = user.LegalEntity.Test, prtn = user.LegalEntity.PartnerId, roles = roles });
            }

            return Ok(new LoginDto() { info = "", error = "", auth = "", sess = "", oper = "", lang = "", cntr = "", paym = false, prtn = null, roles = roles });
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
            await _registerClient.Initialize(_legalEntity);
            var properties = await _registerClient.GetProperties();

            List<PropertyDto> data = properties.Select(a => { var b = _mapper.Map<Property, PropertyDto>(a); b.LegalEntityName = a.LegalEntity.Name; return b; }).ToList();

            return Json(data);
        }

        [HttpGet]
        [Route("propertiesExternal")]
        public async Task<ActionResult<List<PropertyDto>>> PropertiesExternal()
        {            
            var result = await _registerClient.Properties(_legalEntity);

            var data = db.Properties.Where(a => a.LegalEntityId == _appUser!.LegalEntityId).ToList()
                .Select(a => { var b = _mapper.Map<Property, PropertyDto>(a); b.LegalEntityName = a.LegalEntity.Name; return b; }).ToList();

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
        public async Task<ActionResult<List<ItemDto>>> Itmes()
        {            
            return Json(db.Items.Where(a => a.LegalEntityId == _appUser.LegalEntityId).Select(a => new ItemDto { Id = a.Id, Name = a.Name, Unit = a.Unit, Code = a.Code, VatRate = a.VatRate, Price = a.Price, PriceInclVat = a.PriceInclVat, Description = a.Description, LegalEntityId = a.LegalEntityId, VatExempt = a.VatExempt.ToString() }));
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
            //await _registerClient.Authenticate(_legalEntity);
            await _registerClient.Initialize(_legalEntity);
            var legalEntities = await _registerClient.GetLegalEntities();
            var ids = legalEntities.Select(a => a.Id).ToArray();

            var grupe = db.Groups.Include(a => a.Property).Where(a => ids.Contains(a.Property.LegalEntityId))
                .OrderByDescending(a => a.Date)
                .Skip((page - 1) * 50)
                .Take(50)
            //.Select(a => new { a.Id, a.Date, a.PropertyId, a.UnitId, BrojGostiju = db.rb90Persons.Where(b => b.GroupId == a.Id).Count(), Gosti = db.rb90GuestList(a.Id), a.Status }).ToList();
                .Select(a => new GroupEnrichedDto {
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
                    NoOfGuests = a.Persons.Count()
                })
                .ToList();

            var groupIds = grupe.Select(g => g.Id).ToList();
            var statuses = await _groupService.GetPaymentStatusForGroupsAsync(groupIds);

            foreach (var group in grupe)
            {
                group.PaymentStatus = statuses[group.Id];
            }

            return Json(grupe);            
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
            var m = db.Groups.Include(a => a.Persons).Select(a => new GroupEnrichedDto() { 
                Id = a.Id,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                Status = a.Status,
                LegalEntityId = a.LegalEntityId,
                UnitId = a.UnitId,
                GUID = a.Guid,
                Guests = db.GuestList(a.Id),
                PropertyName = a.Property.Name,
                Email = a.Email,
                Date = a.Date,
                PropertyId = a.Property.Id,
                NoOfGuests = a.Persons.Count(),
                ResTaxAmount = a.ResTaxAmount ?? 0,
                ResTaxFee = a.ResTaxFee ?? 0,
                ResTaxCalculated = a.ResTaxCalculated ?? false,
                ResTaxPaid = a.ResTaxPaid ?? false
            }).SingleOrDefault(a => a.Id == id);

            if (m != null)
            {
                var statuses = await _groupService.GetPaymentStatusForGroupsAsync(new List<int> { m.Id });
                m.PaymentStatus = statuses[m.Id];
            }

            return m;
        }


        [HttpGet]
        [Route("groupDelete")]
        public ActionResult<BasicDto> GroupDelete(int id)
        {
            var group = db.Groups.Include(a => a.Persons).SingleOrDefault(a => a.Id == id)!;

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
        [Route("personDelete")]
        public async Task<ActionResult<BasicDto>> PersonDelete(int id)
        {
            Person person = null;

            if (_registerClient is SrbClient)
            {
                person = db.SrbPersons.FirstOrDefault(p => p.Id == id);                
            }

            if (_registerClient is MneClient)
            {
                person = db.MnePersons.FirstOrDefault(p => p.Id == id);                
            }

            var dto = await _registerClient.PersonDelete(person);

            return dto;
        }

        [HttpGet]
        [Route("personExternalDelete")]
        public async Task<ActionResult> PersonExternalDelete(int id)
        {
            Person person = null;

            if (_registerClient is SrbClient)
            {
                person = db.SrbPersons.FirstOrDefault(p => p.Id == id);
            }

            if (_registerClient is MneClient)
            {
                person = db.MnePersons.Include(a => a.Property).Include(a => a.LegalEntity).FirstOrDefault(p => p.Id == id);
                person.IsDeleted = true;
            }

            var result = await _registerClient.RegisterPerson(person, null, null);

            return Json(result);
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
            var group = db.Groups
                .Include(a => a.Property).ThenInclude(a => a.LegalEntity)
                .Include(a => a.Property).ThenInclude(a => a.Municipality)
                .Include(a => a.LegalEntity)
                .Where(a => a.Id == id).First();

            var legalEntity = group.LegalEntity;

            try
            {
                await _registerClient.Initialize(legalEntity);
                var result = await _registerClient.RegisterGroup(group, checkInDate, checkOutDate);
                //if (result != null) Response.StatusCode = 400;

                return Json(result);
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(Exceptions.StringException(e));
            }
        }

        [HttpGet]
        [Route("personRegister")]
        public async Task<ActionResult> PersonRegister(int id)
        {
            try
            {
                Person person = null;
                if (_registerClient is SrbClient)
                {
                    person = db.SrbPersons.Include(a => a.Property).Include(a => a.LegalEntity).FirstOrDefault(p => p.Id == id)!;                    
                }
                if (_registerClient is MneClient)
                {
                    person = db.MnePersons
                        .Include(a => a.Property).ThenInclude(a => a.Municipality)
                        .Include(a => a.Property).ThenInclude(a => a.LegalEntity)
                        .Include(a => a.LegalEntity).FirstOrDefault(p => p.Id == id)!;
                }

                if (User.IsInRole("TouristOrgOperator"))
                {                    
                    if (person != null && (person.ExternalId ?? 0) != 0)
                    {
                        return Json(new BasicDto() { info = "", error = "Nemate prava da vršite izmjene na već prijavljenom gostu" });
                    }
                }

                var result = await _registerClient.RegisterPerson(person, null, null);
                //if (result.ExternalErrors.Any()) Response.StatusCode = 400;
                return Json(result);
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(Exceptions.StringException(e));
            }
        }

		[HttpGet]
		[Route("checkOurSrb")]
		public async Task<ActionResult> CheckOutSrb(int id)
		{
			try
			{
				var person = db.SrbPersons.Include(a => a.Property).Include(a => a.LegalEntity).FirstOrDefault(p => p.Id == id)!;
				await _registerClient.Authenticate(person.Property.LegalEntity);

				person.CheckOut = person.PlannedCheckOut ?? DateTime.Now;
				person.NumberOfServices = (int)(person.CheckOut.Value.Date - person.CheckIn.Value.Date).TotalDays;
				var result = await (_registerClient as SrbClient).CheckOut(person);
				if (result.errors.Any() == false)
				{
					person.CheckedOut = true;
					db.SaveChanges();
				}
				else
				{
					person.CheckOut = null;
					person.CheckedOut = false;
					person.Error = string.Join(", ", result.errors);
					db.SaveChanges();
					_logger.LogError($"SRB Scheduler CheckOut Errors: {person.FirstName} {person.LastName} - {string.Join(", ", result.errors)}");
				}

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
        public ActionResult<List<SrbPersonDto>> PersonSrbList(int id)
        {

            var data = db.SrbPersons.Include(a => a.Property).ThenInclude(a => a.LegalEntity).Include(a => a.LegalEntity).Where(a => a.GroupId == id).ToList().Select(a => 
            {
                var mapped = _mapper.Map<SrbPersonDto>(a);
                mapped.LegalEntityName = a.Property.LegalEntity.Name;
                return mapped;
            }).ToList();

            return Json(_mapper.Map<List<SrbPersonDto>>(db.SrbPersons.Where(a => a.GroupId == id).ToList()));
        }


        [HttpPost]
        [Route("personMneSave")]
        public async Task<ActionResult<MnePersonDto>> PersonMne(MnePersonDto gost)
        {
            _logger.LogDebug("START GOST");

            _logger.LogDebug("AFTER START GOST");

            try
            {
                var g = db.Groups.Where(a => a.Id == gost.GroupId).FirstOrDefault();
                var o = db.Properties.Where(a => a.Id == gost.PropertyId).Include(a => a.LegalEntity).FirstOrDefault();
                //await _registerClient.Initialize(_legalEntity);
                if (User.IsInRole("TouristOrgOperator") || User.IsInRole("TouristOrgAdmin") || User.IsInRole("TouristController"))
                {
                    var pass = o.LegalEntity.PassThroughId;
                    if (pass.HasValue)
                    {
                        var passLegalEntity = db.LegalEntities.FirstOrDefault(a => a.Id == pass.Value);
                        await _registerClient.Initialize(passLegalEntity);
                    }
                }
                else
                {
                    await _registerClient.Initialize(_legalEntity);
                }
                var result = await _registerClient.Person(gost);

                return Json(_mapper.Map<MnePersonDto>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("PersonMneError: " + ex.Message);
                return Json(new BasicDto() { error = ex.Message, info = "" });
            }
        }


        [HttpPost]
        [Route("guestListPdf")]
        public async Task<ActionResult> GuestListPdf(int objekat, string datumod, string datumdo)
        {
            try
            {
                var stream = await _registerClient.GuestListPdf(objekat, datumod, datumdo);

                stream.Seek(0, SeekOrigin.Begin);
                var fsr = new FileStreamResult(stream, "application/pdf");
                fsr.FileDownloadName = $"KnjigaGostiju.pdf";
                return fsr;
            }            
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }


        [HttpPost]
        [Route("guestListMail")]
        public async Task<ActionResult<BasicDto>> GuestListMail(int objekat, string datumod, string datumdo, string email)
        {
            try
            { 
                if (email == null)
                {
                    Response.StatusCode = 400;
                    return Json(new { info = "", error = "Nije unesena e-mail adresa!" });
                }

                await _registerClient.GuestListMail(objekat, datumod, datumdo, email);

                return Json(new { info = "Uspješno poslata knjiga gostiju putem e-maila!", error = "" });
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = Exceptions.StringException(e), id = -1 });
            }
        }


        [HttpPost]
        [Route("personSrbSave")]
        public async Task<ActionResult<SrbPersonDto>> PersonSrb(SrbPersonDto gost)
        {
            try
            {
                _logger.LogDebug("START GOST");
                _logger.LogInformation("GOST JSON: " + JsonSerializer.Serialize(gost));

                //var g = db.Groups.Where(a => a.Id == gost.GroupId).FirstOrDefault();
                var o = db.Properties.Where(a => a.Id == gost.PropertyId).Include(a => a.LegalEntity).FirstOrDefault();
                await _registerClient.Initialize(_legalEntity);
                var result = await _registerClient.Person(gost);

                return Json(_mapper.Map<SrbPersonDto>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("PersonSrbError: " + ex.Message);
                return Json(new BasicDto() { error = ex.Message, info = "" });
            }
        }


        [HttpGet]
        [Route("confirmationGroupMail")]
        public async Task<ActionResult<BasicDto>> ConfirmationGroupMail(int id, string email)
        {
            if (email == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nije unesena e-mail adresa!" });
            }
            var group = db.Groups.Include(a => a.Property).Include(a => a.LegalEntity).Include(a => a.Persons).FirstOrDefault(a => a.Id == id);
            await _registerClient.ConfirmationGroupMail(group, email);

            return Json(new { info = "Uspješno poslate potvrde putem e-maila!", error = "" });
        }

		[HttpGet]
		[Route("confirmationPersonMail")]
		public async Task<ActionResult<BasicDto>> ConfirmationPersonMail(int id, string email)
		{
			if (email == null)
			{
				Response.StatusCode = 400;
				return Json(new { info = "", error = "Nije unesena e-mail adresa!" });
			}

			Person person = null;
			if (_registerClient is SrbClient)
			{
				person = db.SrbPersons.Include(a => a.Property).Include(a => a.LegalEntity).FirstOrDefault(p => p.Id == id)!;
			}
			if (_registerClient is MneClient)
			{
				person = db.MnePersons.Include(a => a.Property).Include(a => a.LegalEntity).FirstOrDefault(p => p.Id == id)!;
			}

			await _registerClient.ConfirmationPersonMail(person, email);

			return Json(new { info = "Uspješno poslate potvrde putem e-maila!", error = "" });
		}


		[HttpGet]
        [Route("certificatePdf")]
        public async Task<ActionResult> CertificatePdf(int? group, int? person)
        {
            try
            {
                Stream stream = null;
                if (group.HasValue && group.Value != 0)
                {
                    var grp = db.Groups.Include(a => a.Property).Include(a => a.Persons).Include(a => a.Property).ThenInclude(a => a.LegalEntity).Where(a => a.Id == group).FirstOrDefault();
                    if (grp == null)
                    {
                        Response.StatusCode = 400;
                        return Json(new { info = "", error = "Nepostojeća prijava gostiju!" });
                    }
                    if (_registerClient is SrbClient) await _registerClient.Authenticate(grp.Property.LegalEntity);
                    if (_registerClient is MneClient) await _registerClient.Initialize(grp.Property.LegalEntity);
                    stream = await _registerClient.ConfirmationGroupPdf(grp);
                }
                else if (person.HasValue && person.Value != 0)
                {
                    Person prs = null;
                    if (_registerClient is SrbClient)
                    {
                        prs = db.SrbPersons.Include(a => a.Property).ThenInclude(a => a.LegalEntity).FirstOrDefault(p => p.Id == person);
                        await _registerClient.Authenticate(prs.Property.LegalEntity);
                    }
                    if (_registerClient is MneClient)
                    {
                        prs = db.MnePersons.Include(a => a.Property).FirstOrDefault(p => p.Id == person);
                        await _registerClient.Initialize(prs.Property.LegalEntity);
                    }                    
                    stream = await _registerClient.ConfirmationPersonPdf(prs);
                }
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
        public async Task<ActionResult<Invoice>> InvoiceCreate(int? property, int? group, Invoice? racun, PaymentType? pay)
        {
            try
            {
                _logger.LogDebug("RACUN START:");
                Property p;
                if (property.HasValue) p = db.Properties.FirstOrDefault(a => a.Id == property)!;
                else p = db.Properties.Where(a => a.LegalEntityId == _appUser.LegalEntityId).FirstOrDefault()!;

                if (group.HasValue && _registerClient is MneClient)
                {
                    var g = db.Groups.Include(a => a.Persons).Include(a => a.Property).FirstOrDefault(a => a.Id == group);
                    if (g.Persons.Any() == false) {
                        return Json(new BasicDto() { info = "", error = "Grupa ne sadrži nijednog gosta, pa se račun ne može automatski napraviti!" });
                    }
                    var rac = await _documentService.CreateInvoice(g, pay);
                    var dto = _documentService.Doc2Invoice(rac);
                    return Json(dto);
                }
                else
                {
					_logger.LogDebug("RACUN JSON: " + JsonSerializer.Serialize(racun));
					var rac = _documentService.CreateInvoice(racun, p);
                    var dto = _documentService.Doc2Invoice(rac);
                    return Json(dto);
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
        public ActionResult<Invoice> InvoiceGet(int id)
        {
            Document doc = db.Documents.Where(a => a.Id == id).FirstOrDefault();

            var rac = _documentService.Doc2Invoice(doc);

            return Json(rac);
        }


        [HttpGet]
        [Route("invoiceList")]
        public ActionResult<Invoice> InvoiceList(int page = 1, string status = "A")
        {
            var docStatus = status switch
            {
                "A" => DocumentStatus.Active,
                "F" => DocumentStatus.Fiscalized,
                "N" => DocumentStatus.NotFiscalized,
                "P" => DocumentStatus.Posted,
                _ => DocumentStatus.None
            };

            var data = db.Documents.Include(a => a.DocumentItems).Include(a => a.DocumentPayments)
                .Where(a => a.LegalEntityId == _appUser.LegalEntityId)
                .Where(a => a.Status == docStatus || status == null || docStatus == DocumentStatus.None)
                .OrderByDescending(a => a.InvoiceDate).Skip((page - 1) * 50).Take(50).ToList();

            var result = new List<Invoice>();

            foreach (var d in data)
            {
                var inv = _documentService.Doc2Invoice(d);
				result.Add(inv);
            }

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
        public ActionResult InvoiceDelete(int id)
        {
            var doc = db.Documents.Where(a => a.Id == id).FirstOrDefault();

            if (doc.Status == DocumentStatus.Fiscalized || doc.Status == DocumentStatus.NotFiscalized) return Json(new { error = "Račun je fiskalizovan ili poslat na fiskalizaciju, pa se ne može brisati", info = "", id = doc.Id });

            try
            {
                db.Documents.Remove(doc);
                db.SaveChanges();
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
            var doc = db.Documents.Include(a => a.DocumentItems).Include(a => a.DocumentPayments).Where(a => a.Id == id).FirstOrDefault();

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
                var storno = _documentService.StornoInvoice(doc);
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
                var reportName = _config["REPORTING:MNE:Invoice"];
                var report = db.Reports.Where(a => a.Name == reportName).FirstOrDefault();
                await _documentService.InvoiceEmail(doc.Id, report.Id, email);
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
            var doc = db.Documents.Include(a => a.LegalEntity).Include(a => a.DocumentItems).ThenInclude(a => a.Item).Include(a => a.DocumentPayments).Where(a => a.Id == id).FirstOrDefault();

            //if (doc.Status != DocumentStatus.Fiscalized)
            //{
            //    Response.StatusCode = 400;
            //    return Json(new { error = "Račun nije fiskalizovan pa se ne može slati na mail", info = "", id = doc.Id });
            //}

            try
            {
                var reportName = _config["REPORTING:MNE:Invoice"];
                if(doc.LegalEntity.InVat == false) reportName = _config["REPORTING:MNE:InvoiceNoVat"];

                var report = db.Reports.Where(a => a.Name == reportName).FirstOrDefault();
                var bytes = await _documentService.InvoicePdf(doc.Id, report.Id);
                return File(bytes, "application/pdf", "report");
                //stream.Seek(0, SeekOrigin.Begin);
                //var fsr = new FileStreamResult(stream, $"{(output == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document")}");
                //fsr.FileDownloadName = $"Faktura br. {doc.No.ToString("0")}.{output}";
                //return fsr;
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
        public ActionResult<ResTaxCalc> ResTaxCalc(int objekat, string datumod, string datumdo)
        {
            var obj = db.Properties.FirstOrDefault(a => a.Id == objekat);            
                        
            var OD = DateTime.ParseExact(datumod, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var DO = DateTime.ParseExact(datumdo, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

            var tax = new ResTaxCalc();
            tax.Status = "A";
            tax.PropertyId = objekat;
            tax.LegalEntityId = obj.LegalEntityId;
            tax.Date = DateTime.Now;
            tax.DateFrom = OD;
            tax.DateTo = DO;
            db.ResTaxCalc.Add(tax);
            db.SaveChanges();

            tax.Items = new List<ResTaxCalcItem>();

            var rb90Client = _registerClient as MneClient;

            rb90Client.CalcResTax(tax, objekat, OD, DO, "FULL", "STRANI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "FULL", "DOMACI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "HALF", "STRANI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "HALF", "DOMACI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "NONE", "STRANI");
            rb90Client.CalcResTax(tax, objekat, OD, DO, "NONE", "DOMACI");

            db.Entry(tax).Collection(a => a.Items).Load();

            tax.Amount = tax.Items.Select(a => a.TotalTax).Sum();
            db.SaveChanges();
            
            return Json(ResTaxDto.FromEntity(tax));
        }


        [HttpPost]
        [Route("resTaxCalcPay")]
        public async Task<ActionResult> ResTaxCalcPay(int group)
        {

            try
            {
                var g = db.Groups.Include(a => a.Persons).FirstOrDefault(a => a.Id == group);
                var rb90Client = _registerClient as MneClient;
                rb90Client.CalcGroupResTax(g);
                var result = await GroupGet(group);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new BasicDto() { error = ex.Message, info = "" });
            } 
        }


        [HttpGet]
        [Route("resTaxList")]
        public ActionResult<List<ResTaxCalc>> ResTaxList(int page = 1)
        {
            var data = db.ResTaxCalc
                .Where(a => a.LegalEntityId == _appUser.LegalEntityId)
                .Include(a => a.Items)
                .OrderByDescending(a => a.Date).Skip((page - 1) * 50).Take(50).ToList()
                .Select(a => ResTaxDto.FromEntity(a));

            return Json(data);
        }

        [HttpPost]
        [Route("resTaxDelete")]
        public ActionResult<BasicDto> ResTaxDelete(int id)
        {
            var tax = db.ResTaxCalc.Where(a => a.Id == id).FirstOrDefault();

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
            var tax = db.ResTaxCalc.Where(a => a.Id == id).FirstOrDefault();

            if (tax == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nepostojeća boravišna taksa!" });
            }

            var obj = db.Properties.Where(a => a.Id == tax.PropertyId).FirstOrDefault();

            try
            {
                var stream = await (_registerClient as MneClient).ResTaxPdf(id);

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

        [HttpPost]
        [Route("registerFcmToken")]
        public async Task<ActionResult> RegisterFcmToken(UserDeviceDto dto)
        {
            var userId = _userManager.GetUserId(HttpContext.User) ?? "";
            var userDevice = new UserDevice() 
            { 
                DeviceId = dto.DeviceId,
                FcmToken = dto.FcmToken,
                LastUpdated = DateTime.Now,
                UserId = userId
            };
            await _fcmService.RegisterFcmToken(userDevice);
            return Ok();
        }

        [HttpPost]
        [Route("sendFcmMessage")]
        public async Task<ActionResult> SendFcmMessage(FcmMessage message)
        {
            await _fcmService.SendFcmMessage(message);
            return Ok();
        }

        [HttpPost]
        [Route("initiatePosPaymentSession")]
        public async Task<ActionResult<InitiatePosPaymentSessionOutput>> InitiatePosPaymentSession(InitiatePosPaymentSessionInput input)
        {
            // check if requested transaction type is allowed
            if (!Enum.TryParse(input.TransactionType, out PaytenTransactionTypes transactionType) ||
               !Enum.IsDefined(typeof(PaytenTransactionTypes), transactionType))
            { 
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nedozvoljeni tip transakcije!" });
            }

            // fetch document from database
            var document = db.Documents.Include(x => x.LegalEntity)
                .Include(x => x.Property)
                .Where(x => x.Id == input.DocumentId && x.LegalEntityId == _legalEntity.Id)
                .FirstOrDefault();

            if (document == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nepostojeći račun!" });
            }

            if (document.Amount == 0m)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = "Iznos ne smije biti 0!" });
            }

            // set user id from property or legalentity
            var userId = !string.IsNullOrEmpty(document.Property.PaytenUserId) ? document.Property.PaytenUserId : document.LegalEntity.PaytenUserId;

            if (string.IsNullOrEmpty(userId))
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nije pronađen ID korisnika!" });
            }

            // authorize - get bearer token
            var applicationLoginId = _configuration["PAYTEN:ApplicationLoginID"];
            var password = _configuration["PAYTEN:Password"];
            var authorizeResult = await _paytenService.Authorize(new AuthorizeRequest () 
            { 
                ApplicationLoginID = applicationLoginId, 
                Password = password
            });
            if (authorizeResult.Item1 == null)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = "Neuspjela autorizacija!" });
            }
            var bearerToken = authorizeResult.Item1.Token;

            // create payment session token based on transaction type
            var callBackUrl = _configuration["PAYTEN:CallBackURL"];
            var paymentSessionTokenResult = new Tuple<CreatePaymentSessionTokenResponse, Error>(null, null);

            if (transactionType == PaytenTransactionTypes.Sale) 
            {
                paymentSessionTokenResult = await _paytenService.CreatePaymentSessionToken(new CreatePaymentSessionTokenRequest()
                {
                    UserHash = userId,
                    Amount = document.Amount,
                    CurrencyCode = _currencyCode,
                    OrderID = document.PaytenOrderId.ToString(),
                    TransactionType = input.TransactionType,
                    CallBackURL = callBackUrl

                }, bearerToken);
            }
            else if  (transactionType == PaytenTransactionTypes.Void)
            {
                paymentSessionTokenResult = await _paytenService.CancelPaymentSessionToken(new CancelPaymentSessionTokenRequest()
                {
                    UserHash = userId,
                    OrderID = document.PaytenOrderId.ToString(),
                    TransactionType = input.TransactionType,
                    CallBackURL = callBackUrl

                }, bearerToken);
            }

            if (paymentSessionTokenResult.Item1 == null)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = "Neuspjela sesija!" });
            }
            var paymentSessionToken = paymentSessionTokenResult.Item1.PaymentSessionToken;

            // create transaction in database
            var transaction = await db.PaymentTransactions.AddAsync(new PaymentTransaction
            {
                DocumentId = document.Id,
                LegalEntityId = document.LegalEntityId,
                PropertyId = document.PropertyId,
                Token = paymentSessionToken,
                Type = input.TransactionType,
                Amount = document.Amount,
                UserCreated = _legalEntity.Name,
                UserCreatedDate = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            // return payment token and transaction id
            return Json(new InitiatePosPaymentSessionOutput
            {
                PaymentSessionToken = paymentSessionToken,
                TransactionId = transaction.Entity.Id
            });
        }

        [HttpPost]
        [Route("storePosPaymentResult")]
        public async Task<ActionResult<bool>> StorePosPaymentResult(StorePosPaymentResultInput input)
        {
            // fetch transaction from database
            var transaction = await db.PaymentTransactions
                .Where(x => x.Id == input.TransactionId && x.LegalEntityId == _legalEntity.Id)
                .FirstOrDefaultAsync();

            if (transaction == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nepostojeća transakcija!" });
            }

            // update transaction with payment result
            transaction.Status = input.Status;
            transaction.Success = input.Success;
            transaction.UserModified = _legalEntity.Name;
            transaction.UserModifiedDate = DateTime.UtcNow;

            // save changes
            await db.SaveChangesAsync();

            return Json(new { info = "Rezultat transakcije je uspješno sačuvan!", error = "" });
        }

        [HttpPost]
        [Authorize]
        [Route("initiatePayment")]
        public async Task<ActionResult<InitiatePaymentOutput>> InitiatePayment(InitiatePaymentInput input)
        {
            if (_legalEntity == null)
            {
                Response.StatusCode = 401;
                return Json(new { info = "", error = "Korisnik nije ulogovan!" });
            }

            var group = await db.Groups
                .Include(x => x.LegalEntity)
                .Include(x => x.Property)
                .ThenInclude(x => x.LegalEntity)
                .Where(x => x.Id == input.GroupId /*&& x.LegalEntityId == _legalEntity.Id*/)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                Response.StatusCode = 400;
                return Json(new { info = "", error = "Nije pronađen ID grupe!" });
            }

            if (!group.ResTaxAmount.HasValue ||
                group.ResTaxAmount.Value == 0)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = "Iznos ne smije biti 0!" });
            }

            group.ResTaxFee = await _groupService.CalculateResTaxFee(group);

            var transactionId = Guid.NewGuid().ToString();

            var names = (_legalEntity.Name ?? string.Empty).Trim().Split(' ');
            var firstName = names.Length > 1 ? string.Join(" ", names.Take(names.Length - 1)) : names.FirstOrDefault() ?? string.Empty;
            var lastName = names.Length > 1 ? names.Last() : string.Empty;

            var paymentResponse = await _paymentService.InitiatePaymentAsync(new PaymentServiceRequest
            {
                MerchantTransactionId = transactionId,
                Amount = group.ResTaxAmount.Value + group.ResTaxFee.Value,
                SurchargeAmount = group.ResTaxFee.Value,
                TransactionToken = input.Token,
                SuccessUrl = input.SuccessUrl,
                CancelUrl = input.CancelUrl,
                ErrorUrl = input.ErrorUrl,
                WithRegister = input.StoreCard,
                ReferenceUuid = input.ReferenceUuid,
                TestMode = _legalEntity.Test,
                FirstName = firstName,
                LastName = lastName,
                BillingAddress1 = _legalEntity.Address,
                Identification = _legalEntity.TIN,
                Email = _appUser.Email!
            });

            var now = DateTime.UtcNow;
            var transaction = await db.PaymentTransactions.AddAsync(new PaymentTransaction
            {
                Status = paymentResponse.ReturnType,
                Success = paymentResponse.Success,
                MerchantTransactionId = transactionId,
                GroupId = group.Id,
                LegalEntityId = group.Property.LegalEntityId,
                PropertyId = group.PropertyId,
                Token = input.Token,
                Type = PaymentTransactionTypes.DEBIT.ToString(),
                Amount = group.ResTaxAmount.Value,
                SurchargeAmount = group.ResTaxFee.Value,
                UserCreated = _legalEntity.Name,
                UserCreatedDate = now,
                WithRegister = input.StoreCard,
                ReferenceUuid = paymentResponse.Uuid
            });

            await db.SaveChangesAsync();

            if (paymentResponse.Success)
            {
                return Json(new InitiatePaymentOutput
                {
                    RedirectUrl = paymentResponse.RedirectUrl,
                    RedirectType = paymentResponse.RedirectType
                });
            }
            else
            {
                var errors = paymentResponse.Errors?.Select(x => x.AdapterMessage ?? x.ErrorMessage);
                return Json(new { info = "", error = errors });
            }
        }

        [HttpPost]
        [Route("storePaymentResult")]
        public async Task<ActionResult<bool>> StorePaymentResult([FromQuery] bool testMode = false)
        {
            try
            {
                // Read the request body
                string requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
                JObject requestBodyObject = JObject.Parse(requestBody);
                var apiKey = _paymentService.GetConfigurationValue(testMode, "ApiKey");
                var requestUri = $"{Request.Path}{Request.QueryString}";
                var dateHeader = Request.Headers["Date"].FirstOrDefault();
                var xSignatureHeader = Request.Headers["X-Signature"].FirstOrDefault();

                if (string.IsNullOrEmpty(dateHeader) || string.IsNullOrEmpty(xSignatureHeader))
                {
                    return BadRequest("Missing required headers.");
                }

                bool isValidSignature = _paymentService.ValidateSignature(requestBody, requestUri, dateHeader, xSignatureHeader, testMode);
                if (!isValidSignature)
                {
                    return Unauthorized("Invalid signature.");
                }

                // fetch transaction from database
                string transactionId = requestBodyObject.SelectToken("merchantTransactionId")?.ToString();
                var transaction = await db.PaymentTransactions
                    .Include(x => x.LegalEntity)
                    .Where(x => x.MerchantTransactionId == transactionId)
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    Response.StatusCode = 400;
                    return NotFound("Transaction not found.");
                }

                // update transaction with payment result
                var result = requestBodyObject.SelectToken("result")?.ToString();
                var success = result == PaymentResponseTypes.OK.ToString();

                transaction.Status = result;
                transaction.Success = success;
                var now = DateTime.UtcNow;
                transaction.ResponseJson = requestBody;
                transaction.UserModifiedDate = now;

                var transactionType = requestBodyObject.SelectToken("transactionType")?.ToString();
                var lastFourDigits = requestBodyObject.SelectToken("returnData.lastFourDigits")?.ToString();
                var cardType = requestBodyObject.SelectToken("returnData.type")?.ToString();

                // If status is OK, update ResTaxPaid field on the associated group to true
                if (success && transaction.GroupId.HasValue)
                {
                    var group = await db.Groups.FindAsync(transaction.GroupId.Value);
                    if (group != null)
                    {
                        group.ResTaxPaid = true;
                    }
                }
                // If status is OK and transaction type is PREAUTHORIZE, void the amount, deregister the old payment method and update PaymentMethods table
                if (success && 
                    (transactionType == PaymentTransactionTypes.PREAUTHORIZE.ToString() ||
                    transaction.WithRegister == true))
                {
                    if(transactionType == PaymentTransactionTypes.PREAUTHORIZE.ToString())
                    {
                        VoidTransactionInput input = new VoidTransactionInput
                        {
                            ReferenceUuid = transaction.ReferenceUuid!
                        };
                        _ = await VoidTransactionInternal(input, transaction.LegalEntityId!.Value, transaction.LegalEntity.Name, transaction.LegalEntity.Test);
                    }

                    var oldPaymentMethod = await db.PaymentMethods
                        .Include(x => x.PaymentTransaction)
                        .Where(x => x.LegalEntityId == transaction.LegalEntityId)
                        .FirstOrDefaultAsync();

                    if (oldPaymentMethod != null)
                    {
                        DeregisterPaymentMethodInput input = new DeregisterPaymentMethodInput
                        {
                            ReferenceUuid = oldPaymentMethod.PaymentTransaction.ReferenceUuid!
                        };
                        _ = await DeletePaymentMethodInternal(input, transaction.LegalEntityId!.Value, transaction.LegalEntity.Name, transaction.LegalEntity.Test);
                    }

                    var paymentMethod = await db.PaymentMethods.AddAsync(new PaymentMethod
                    {
                        PaymentTransactionId = transaction.Id,
                        LegalEntityId = transaction.LegalEntityId,
                        Type = cardType!,
                        LastFourDigits = lastFourDigits!,
                        UserCreated = transaction.LegalEntity.Name,
                        UserCreatedDate = now,
                    });
                }

                // save changes
                await db.SaveChangesAsync();

                try
                {
                    // if transaction type is DEBIT, send payment confirmation email
                    // additionally we're making sure to send only for successful transactions, but unsuccesful transactions are also supported
                    if (success && transactionType == PaymentTransactionTypes.DEBIT.ToString())
                    {
                        var email = await db.Users
                            .Where(x => x.LegalEntityId == transaction.LegalEntityId)
                            .Select(x => x.Email)
                            .FirstOrDefaultAsync();

                        var amount = requestBodyObject.SelectToken("totalAmount")?.ToString() ?? "N/A";
                        var currency = requestBodyObject.SelectToken("currency")?.ToString() ?? "N/A";
                        var authCode = requestBodyObject.SelectToken("extraData.authCode")?.ToString() ?? string.Empty;
                        lastFourDigits = lastFourDigits ?? "N/A";
                        cardType = cardType ?? "N/A";

                        var template = _configuration["SendGrid:Templates:PaymentConfirmation"]!;
                        var senderEmail = _configuration["SendGrid:EmailAddress"];

                        await _eMailService.SendMail(senderEmail!, email!, template, new
                        {
                            subject = $@"donotreply: Informacije o Vašoj transakciji",
                            status = success ? $"Uspješno plaćanje" : $"Neuspješno plaćanje",
                            orderNumber = $"{transaction.MerchantTransactionId}",
                            amount = $"{amount} {currency}",
                            cardType = $"{cardType}",
                            lastFourDigits = $"{lastFourDigits}",
                            authCode = $"{authCode}",
                            timestamp = $"{transaction.UserCreatedDate.AddHours(2):yyyy-MM-dd HH:mm:ss}", // Format the timestamp,
                            success = success
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send paymnent confirmation email.");
                }

                return Content("OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment result.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet]
        [Route("groupGetPaymentInfo")]
        public async Task<ActionResult<GroupPaymentInfoDto>> GroupGetPaymentInfo(int id)
        {
            if (_legalEntity == null)
            {
                Response.StatusCode = 401;
                return Json(new { info = "", error = "Korisnik nije ulogovan!" });
            }

            var group = await db.Groups.Where(x => x.Id == id /*&& x.LegalEntityId == _legalEntity.Id*/).FirstOrDefaultAsync();

            if (group == null)
            {
                Response.StatusCode = 500;
                return Json(new { info = "", error = "Nije pronađen ID grupe!" });
            }

            // Fetch payment information for the group
            var paymentInfo = await _groupService.GetPaymentInfoForGroupAsync(id);
            return Json(paymentInfo);
        }

        [HttpPost]
        [Route("storePaymentMethod")]
        public async Task<ActionResult<InitiatePaymentOutput>> StorePaymentMethod(RegisterPaymentMethodInput input)
        {
            if (_legalEntity == null)
            {
                Response.StatusCode = 401;
                return Json(new { info = "", error = "Korisnik nije ulogovan!" });
            }

            var transactionId = Guid.NewGuid().ToString();

            var names = (_legalEntity.Name ?? string.Empty).Trim().Split(' ');
            var firstName = names.Length > 1 ? string.Join(" ", names.Take(names.Length - 1)) : names.FirstOrDefault() ?? string.Empty;
            var lastName = names.Length > 1 ? names.Last() : string.Empty;

            var paymentResponse = await _paymentService.PreauthorizeTransaction(new PaymentServiceRequest
            {
                MerchantTransactionId = transactionId,
                TransactionToken = input.Token,
                SuccessUrl = input.SuccessUrl,
                CancelUrl = input.CancelUrl,
                ErrorUrl = input.ErrorUrl,
                TestMode = _legalEntity.Test,
                FirstName = firstName,
                LastName = lastName,
                BillingAddress1 = _legalEntity.Address,
                Identification = _legalEntity.TIN,
                Amount = 0.01m,
                Email = _appUser.Email!
            });

            var now = DateTime.UtcNow;
            var transaction = await db.PaymentTransactions.AddAsync(new PaymentTransaction
            {
                Status = paymentResponse.ReturnType,
                Success = paymentResponse.Success,
                MerchantTransactionId = transactionId,
                LegalEntityId = _legalEntity.Id,
                Token = input.Token,
                Type = PaymentTransactionTypes.PREAUTHORIZE.ToString(),
                UserCreated = _legalEntity.Name,
                UserCreatedDate = now,
                ReferenceUuid = paymentResponse.Uuid,
                Amount = 0.01m
            });

            await db.SaveChangesAsync();

            if (paymentResponse.Success)
            {
                return Json(new InitiatePaymentOutput
                {
                    RedirectUrl = paymentResponse.RedirectUrl,
                    RedirectType = paymentResponse.RedirectType
                });
            }
            else
            {
                var errors = paymentResponse.Errors?.Select(x => x.AdapterMessage ?? x.ErrorMessage);
                return Json(new { info = "", error = errors });
            }
        }

        [HttpPost]
        [Route("deletePaymentMethod")]
        public async Task<ActionResult<InitiatePaymentOutput>> DeletePaymentMethod(DeregisterPaymentMethodInput input)
        {
            if (_legalEntity == null)
            {
                Response.StatusCode = 401;
                return Json(new { info = "", error = "Korisnik nije ulogovan!" });
            }

            return await DeletePaymentMethodInternal(input, _legalEntity.Id, _legalEntity.Name, _legalEntity.Test);
        }

        private async Task<ActionResult<InitiatePaymentOutput>> VoidTransactionInternal(VoidTransactionInput input, int legalEntityId, string legalEntityName, bool testMode)
        {
            var transactionId = Guid.NewGuid().ToString();

            var paymentResponse = await _paymentService.VoidTransaction(new PaymentServiceRequest
            {
                MerchantTransactionId = transactionId,
                ReferenceUuid = input.ReferenceUuid,
                TestMode = testMode
            });

            var now = DateTime.UtcNow;
            var transaction = await db.PaymentTransactions.AddAsync(new PaymentTransaction
            {
                Status = paymentResponse.ReturnType,
                Success = paymentResponse.Success,
                MerchantTransactionId = transactionId,
                LegalEntityId = legalEntityId,
                Type = PaymentTransactionTypes.VOID.ToString(),
                UserCreated = legalEntityName,
                UserCreatedDate = now,
                ReferenceUuid = input.ReferenceUuid
            });

            await db.SaveChangesAsync();

            if (paymentResponse.Success)
            {
                return Ok();
            }
            else
            {
                var errors = paymentResponse.Errors?.Select(x => x.AdapterMessage ?? x.ErrorMessage);
                return Json(new { info = "", error = errors });
            }
        }

        private async Task<ActionResult<InitiatePaymentOutput>> DeletePaymentMethodInternal(DeregisterPaymentMethodInput input, int legalEntityId, string legalEntityName, bool testMode)
        {
            var transactionId = Guid.NewGuid().ToString();

            var paymentResponse = await _paymentService.DeregisterPaymentMethod(new PaymentServiceRequest
            {
                MerchantTransactionId = transactionId,
                ReferenceUuid = input.ReferenceUuid,
                TestMode = testMode
            });

            _ = await db.PaymentMethods
                .Where(x => x.LegalEntityId == legalEntityId)
                .ExecuteDeleteAsync();

            var now = DateTime.UtcNow;
            var transaction = await db.PaymentTransactions.AddAsync(new PaymentTransaction
            {
                Status = paymentResponse.ReturnType,
                Success = paymentResponse.Success,
                MerchantTransactionId = transactionId,
                LegalEntityId = legalEntityId,
                Type = PaymentTransactionTypes.DEREGISTER.ToString(),
                UserCreated = legalEntityName,
                UserCreatedDate = now,
                ReferenceUuid = input.ReferenceUuid
            });

            await db.SaveChangesAsync();

            if (paymentResponse.Success)
            {
                return Ok();
            }
            else
            {
                var errors = paymentResponse.Errors?.Select(x => x.AdapterMessage ?? x.ErrorMessage);
                return Json(new { info = "", error = errors });
            }
        }

        [HttpGet]
        [Route("getPaymentMethod")]
        public async Task<ActionResult<PaymentMethodDto>> GetPaymentMethod()
        {
            if (_legalEntity == null)
            {
                Response.StatusCode = 401;
                return Json(new { info = "", error = "Korisnik nije ulogovan!" });
            }

            var paymentMethod = await db.PaymentMethods.Where(x => x.LegalEntityId == _legalEntity.Id)
                .Include(x => x.PaymentTransaction)
                .OrderByDescending(x => x.UserCreatedDate)
                .FirstOrDefaultAsync();

            if (paymentMethod == null)
            {
                return Json(null);
            }

            var result = new PaymentMethodDto
            {
                ReferenceUuid = paymentMethod.PaymentTransaction.ReferenceUuid!,
                Type = paymentMethod.Type,
                LastFourDigits = paymentMethod.LastFourDigits
            };

            return Json(result);
        }
    }
}