using AutoMapper;
using Humanizer;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Oblak;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Helpers;
using Oblak.Models.Api;
using Oblak.Models.rb90;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using SQLitePCL;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace RegBor.Controllers
{
	public class GroupController : Controller
	{
        private readonly Register _registerClient;        
		private readonly ApplicationDbContext _db;
		private readonly ILogger<GroupController> _logger;
		private readonly IMapper _mapper;
		private readonly ApplicationUser _appUser;
		private readonly int _legalEntityId;
        private readonly HttpContext _context;
		

		public GroupController(
			IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,            
			ApplicationDbContext db,
			IMapper mapper,
			ILogger<GroupController> logger
			)
		{
			_db = db;			
			_logger = logger;
			_mapper = mapper;
            _context = httpContextAccessor.HttpContext!;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
				_legalEntityId = _appUser.LegalEntityId;
                if (_appUser.LegalEntity.Country == CountryEnum.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == CountryEnum.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
            }
        }

        [HttpGet]
		[Route("groups", Name = "Groups")]
		public ActionResult Index(bool nautical = false)
		{
            ViewBag.Nautical = nautical;

            var username = _context!.User!.Identity!.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

            var partnerId = appUser.PartnerId ?? appUser.LegalEntity.PartnerId;
            ViewBag.PartnerId = partnerId;

            var partner = _db.Partners.Where(a => a.Id == partnerId).FirstOrDefault();

            var sl = _db.ResTaxPaymentTypes
                .Where(a => a.PartnerId == partnerId)
                .Select(a =>
                    new SelectListItem() { Text = a.Description, Value = a.Id.ToString() }
                ).ToList();

            ViewBag.ResTaxPaymentTypes = new SelectList(sl, "Value", "Text");

            var sl2 = _db.CodeLists
                .Where(a => a.Country == "MNE" && a.Type == "prelaz")
                .Select(a =>
                    new SelectListItem() { Text = a.Name, Value = a.ExternalId.ToString() }
                ).ToList();


            ViewBag.EntryPoints = new SelectList(sl2, "Value", "Text");

            ViewBag.EntryPoint = partner.UseEntryPointInGroup;

            return View();
		}


        [Route("edit-group")]
        [HttpGet]
        public ActionResult EditGroup(bool nautical = false, int? id = null)
        {
            ViewBag.Nautical = nautical;

            var username = _context!.User!.Identity!.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

            var partner = _db.Partners.Where(a => a.Id == appUser.PartnerId).FirstOrDefault();

            var sl = _db.ResTaxPaymentTypes
                .Where(a => a.PartnerId == appUser.PartnerId)
                .Select(a =>
                    new SelectListItem() { Text = a.Description, Value = a.Id.ToString() }
                ).ToList();

            ViewBag.ResTaxPaymentTypes = new SelectList(sl, "Value", "Text");

            var sl2 = _db.CodeLists
                .Where(a => a.Country == "MNE" && a.Type == "prelaz")
                .Select(a =>
                    new SelectListItem() { Text = a.Name, Value = a.ExternalId.ToString() }
                ).ToList();

            ViewBag.EntryPoints = new SelectList(sl2, "Value", "Text");

            ViewBag.EntryPoint = partner.UseEntryPointInGroup;

            GroupEnrichedDto dto = null;

            if (id.HasValue)
            {
                var g = _db.Groups.FirstOrDefault(a => a.Id == id);
                dto = _mapper.Map<GroupEnrichedDto>(g);
            }
            else
            {
                dto = new GroupEnrichedDto();
            }

            ViewBag.Dto = dto;
            return PartialView();
        }

        [Route("save-group")]
        [HttpPost]
        public ActionResult Create(GroupDto groupDto, [DataSourceRequest] DataSourceRequest request)
       {
            try
            {
                //var property = _db.Properties.Where(p => p.Id == groupDto.PropertyId).First();

                // Map GroupDto properties to your Group entity properties
                var newGroup = new Group
                {
                    Date = DateTime.Now,
                    CheckIn = groupDto.CheckIn,
                    CheckOut = groupDto.CheckOut,
                    Email = groupDto.Email,
                    // Map other properties as needed
                    PropertyId = groupDto.PropertyId ?? 0,
                    UnitId = groupDto.UnitId,
                    LegalEntityId = _legalEntityId,
                    VesselId = groupDto.VesselId,
                    EntryPoint = groupDto.EntryPoint,
                    EntryPointDate = groupDto.EntryPointDate,
                    NauticalLegalEntityData = groupDto.NauticalLegalEntityData,
                    NauticalLegalEntityId = groupDto.NauticalLegalEntityId,
                    ResTaxPaymentTypeId = groupDto.ResTaxPaymentTypeId,
                    Guid = Guid.NewGuid().ToString(),
                    PropertyExternalId = groupDto.PropertyId ?? 0,
                };

                if (_appUser != null && newGroup.CheckInPointId == null)
                {
                    newGroup.CheckInPointId = _appUser.CheckInPointId;
                }

                //if (ModelState.IsValid)
                //{
                _db.Groups.Add(newGroup);
                _db.SaveChanges();
                //}

                _db.Entry(newGroup).Reference(a => a.Property).Load();
                _db.Entry(newGroup).Reference(a => a.ResTaxPaymentType).Load();
                _db.Entry(newGroup).Collection(a => a.Persons).Load();

                var taxstatus = TaxPaymentStatus.AlreadyPaid;
                if (newGroup.ResTaxPaymentType != null) taxstatus = newGroup.ResTaxPaymentType.PaymentStatus;

                (_registerClient as MneClient)!.CalcGroupResTax(newGroup, taxstatus);

                // Return success or any other relevant information
                return Json(new[] { newGroup.Id });
            }
            catch (Exception ex)
            {
                // Handle exceptions and return error information
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpGet]
        [Route("nautical-groups", Name = "NauticalGroups")]
        public ActionResult Index()
        {
            return Index(true);
        }

        /*
		[HttpGet]
		[Route("new-group", Name = "NewGroup")]
		public ActionResult newgrp()
		{
			var objekti = _db.Properties.Where(a => a.LegalEntityId == _company).ToList();
			var jedinice = _db.PropertyUnits.Where(a => a.LegalEntityId == _company).Any();

			if (objekti.Count == 1 && jedinice == false)
			{
				return RedirectToAction("NovaGrupa", new { objekat = objekti.First().Id, jedinica = (int?)null });
			}
			else
			{
				return PartialView();
			}
		}
		*/
        [HttpGet]
		[Route("guest-link", Name = "NewGroup")]
		public ActionResult sendLink()
		{
			return PartialView();
		}

		public ActionResult NovaGrupa(int objekat, int? jedinica, string dolazak, string odlazak, string email, int? vessel, int? nautical)
		{
            Group? g = null;

            if (vessel.HasValue)
            {
                g = _db.Groups.Where(a => a.PropertyId == objekat)
                    .Where(a => a.VesselId == vessel)
                    .Where(a => a.CheckIn == null && a.CheckOut == null && _db.MnePersons.Where(b => b.GroupId == a.Id).Any() == false)
                    .OrderByDescending(a => a.Date).FirstOrDefault();
            }
            else
            {
                g = _db.Groups.Where(a => a.PropertyId == objekat)
                    .Where(a => a.UnitId == jedinica || jedinica == null)
                    .Where(a => a.CheckIn == null && a.CheckOut == null && _db.MnePersons.Where(b => b.GroupId == a.Id).Any() == false)
                    .OrderByDescending(a => a.Date).FirstOrDefault();
            }

			if (g == null)
			{
				g = new Group();
				_db.Groups.Add(g);
			}

			DateTime? d = null;
			DateTime? o = null;

			try
			{
				d = DateTime.ParseExact(dolazak, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
			}
			catch { }

			try
			{
				o = DateTime.ParseExact(odlazak, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
			}
			catch { }

			var property = _db.Properties.FirstOrDefault(a => a.Id == objekat);

			g.LegalEntityId = property.LegalEntityId;
			g.PropertyId = objekat;
			g.UnitId = jedinica;
            g.VesselId = vessel;
            g.NauticalLegalEntityId = nautical;
			g.Email = email;
			g.CheckIn = d;
			g.CheckOut = o;
			g.Date = DateTime.Now;
			g.Status = "A";
			_db.SaveChanges();
			return Json(new { Grupa = g.Id });
		}

		//[HttpGet]
		//[Route("properties", Name = "Properties")]
		//public JsonResult rbobjekti()
		//{
		//	return Json(_db.Properties.Where(a => a.LegalEntityId == _company).ToList());
		//}

		[HttpGet]
		[Route("units", Name = "Units")]
		public JsonResult rbjedinice(int? objekat)
		{
			var jedinice = _db.PropertyUnits.Where(a => a.LegalEntityId == _legalEntityId).ToList();

			if (objekat != null)
			{
				jedinice = jedinice.Where(p => p.PropertyId == objekat.Value).ToList();
			}

			return Json(jedinice);
		}

		public ActionResult ogrp(int g)
		{
			var grupa = _db.Groups.Where(a => a.Id == g).FirstOrDefault();

			ViewBag.Grupa = g;
			ViewBag.Email = grupa.Email ?? "";
			return View();
		}
		

		private async Task<List<Property>> GetProperties()
		{
			var isPropertyAdmin = User.IsInRole("PropertyAdmin");
			var legalEntityId = _appUser!.LegalEntityId;

			List<Property> properties = null;

			if (isPropertyAdmin)
			{
				var ids = _db.LegalEntities.Where(a => a.AdministratorId == legalEntityId).Select(a => a.Id).ToList();
				properties = _db.Properties.Where(a => ids.Contains(a.LegalEntityId)).ToList();
			}
			else
			{
				properties = _db.Properties.Where(a => a.LegalEntityId == legalEntityId).ToList();					
			}

			return properties;
		}

		public static void CalcPeriod(DateTime now, string period, string start, string end, out DateTime Od, out DateTime Do)
		{
			if (period == "D")
			{
				Od = now.Date;
				Do = now.Date.AddDays(1).AddSeconds(-1);
			}
			else if (period == "W")
			{
				Od = now.StartOfWeek(DayOfWeek.Monday);
				Do = now.EndOfWeek(DayOfWeek.Monday);
			}
			else if (period == "M")
			{
				Od = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
				Do = Od.AddMonths(1).AddSeconds(-1);
			}
			else if (period == "Y")
			{
				Od = new DateTime(now.Year, 1, 1, 0, 0, 0);
				Do = Od.AddMonths(12).AddSeconds(-1);
			}
			else if (period == "C")
			{
				if ((start ?? "") != "") Od = DateTime.ParseExact(start, "dd.MM.yyyy", CultureInfo.InvariantCulture);
				else Od = now;

				if ((end ?? "") != "") Do = DateTime.ParseExact(end, "dd.MM.yyyy", CultureInfo.InvariantCulture).Date.Add(new TimeSpan(23, 59, 59));
				else Do = now;
			}
			else
			{
				Od = now;
				Do = now;
			}
		}

		public virtual async Task<ActionResult> Read([DataSourceRequest] DataSourceRequest request, bool nautical = false, string period = "D", string dtmod = null, string dtmdo = null)
        {
			//var isPropertyAdmin = User.IsInRole("PropertyAdmin");
			//var legalEntityId = _appUser!.LegalEntityId;

			//List<PropertyDto> properties = null;

			//if (isPropertyAdmin)
			//{
			//	var ids = _db.LegalEntities.Where(a => a.AdministratorId == legalEntityId).Select(a => a.Id).ToList();
			//	properties = _db.Properties.Where(a => ids.Contains(a.LegalEntityId)).ToList()
			//		.Select(a => _mapper.Map<Property, PropertyDto>(a)).ToList();
			//}
			//else
			//{
			//	properties = _db.Properties.Where(a => a.LegalEntityId == legalEntityId).ToList()
			//		.Select(a => _mapper.Map<Property, PropertyDto>(a)).ToList();
			//}

			//var propertyIds = (await GetProperties()).Select(a => a.Id).ToArray();
            //propertyIds = properties.Select(a => a.Id).ToArray();

            IQueryable<GroupEnrichedDto> data = null;

            if (nautical == false)
            {
                var groups = _db.Groups
                    .Include(a => a.Property).ThenInclude(a => a.LegalEntity)
                    .Include(a => a.LegalEntity)
                    .Include(a => a.Persons)
                    .Where(a => true);


				if (User.IsInRole("TouristOrgOperator"))
                {
                    groups = groups.Where(a => a.CheckInPointId == _appUser.CheckInPointId);
                }

                if (_appUser.UserName.Equals("CrnaGoraAdmin")) //nalog CrnaGora moze da vidi sve grupe koje su kreirali LE za Partner.a Crna Gora
                {
                    groups = groups.Where(a => a.Property.LegalEntity.PartnerId == 2);
                }
                else
                {
                    groups = groups.Where(a => a.LegalEntityId == _legalEntityId);
                }

                DateTime now = DateTime.Now;
				DateTime Od = now.Date;
				DateTime Do = now.Date;

				CalcPeriod(now, period, dtmod, dtmdo, out Od, out Do);

				data = groups
					//.Where(a => propertyIds.Contains(a.Id))
					.Where(a => a.UserCreatedDate >= Od && a.UserCreatedDate <= Do)
                    .Where(a => a.VesselId == null)
                    .Include(a => a.CheckInPoint)
                    .Include(a => a.ResTaxPaymentType)
                    .OrderByDescending(x => x.Date)
                    .Select(a => new GroupEnrichedDto
                    {
                        Id = a.Id,
                        Date = a.Date,
                        PropertyName = a.Property.PropertyName,
                        VesselDesc = "",
                        NauticalLegalEntity = "",
                        LegalEntity = a.Property.LegalEntity.Name,
                        CheckIn = a.CheckIn,
                        CheckOut = a.CheckOut,
                        Email = a.Email,
                        ResTaxAmount = a.ResTaxAmount ?? 0m,
                        ResTaxFee = a.ResTaxFee ?? 0m,                        
                        Guests = _db.GuestList(a.Id),
                        UserCreated = a.UserCreated,
                        CheckInPointName = a.CheckInPoint == null ? "" : a.CheckInPoint.Name,
                        ResTaxPaymentTypeName = a.ResTaxPaymentType == null ? "" : a.ResTaxPaymentType.Description,
                        Registered = a.Persons.Any() && 
                                     a.Persons.All(p => p.ExternalId != null && p.ExternalId > 0)
                    });
            }
            else
            {
                var groups = _db.Groups
                    .Include(a => a.Persons)
                    .Where(a => true);

                if (User.IsInRole("TouristOrgOperator"))
                {
                    groups = groups.Where(a => a.CheckInPointId == _appUser.CheckInPointId);
                }

                data = groups
                    //.Where(a => propertyIds.Contains(a.Id))
                    .Where(a => a.VesselId != null)
                    .Where(a => a.LegalEntityId == _legalEntityId)
                    .Include(a => a.Property)
                    .ThenInclude(a => a.LegalEntity)
                    .Include(a => a.Vessel)
                    .Include(a => a.NauticalLegalEntity)
                    .Include(a => a.LegalEntity)
                    .Include(a => a.CheckInPoint)
                    .Include(a => a.ResTaxPaymentType)
                    .OrderByDescending(x => x.Date)
                    .Select(a => new GroupEnrichedDto
                    {
                        Id = a.Id,
                        Date = a.Date,
                        PropertyName = a.Property.PropertyName,
                        VesselDesc = a.Vessel.Name + ", " + a.Vessel.Registration,
                        //NauticalLegalEntity = a.NauticalLegalEntity.Name,
                        NauticalLegalEntityData = a.NauticalLegalEntityData,
                        LegalEntity = a.Property.LegalEntity.Name,
                        CheckIn = a.CheckIn,
                        CheckOut = a.CheckOut,
                        Email = a.Email,     
                        ResTaxAmount = a.ResTaxAmount ?? 0m,
                        ResTaxFee = a.ResTaxFee ?? 0m,
                        Guests = _db.GuestList(a.Id),
                        CheckInPointName = a.CheckInPoint == null ? "" : a.CheckInPoint.Name,
                        ResTaxPaymentTypeName = a.ResTaxPaymentType == null ? "" : a.ResTaxPaymentType.Description,
                        Registered = a.Persons.Any() && 
                                     a.Persons.All(p => p.ExternalId != null && p.ExternalId > 0)
                    });
            }

            return Json(data.ToDataSourceResult(request));
        }

        public async Task<ActionResult> GetPropertyList()
        {
            var properties = await GetProperties();
            return Json(properties);
        }

        public ActionResult GetPropertyUnits(int propertyId)
        {
            var propertyUnits = _db.PropertyUnits.Where(p => p.PropertyId == propertyId).ToList();
            return Json(propertyUnits);
        }

        

        [HttpGet("group-desc")]        
        public ActionResult Desc(int groupId)
        {
            try
            { 
                var g = _db.Groups.Include(a => a.Property).ThenInclude(a => a.LegalEntity).Include(a => a.ResTaxPaymentType).Include(a => a.Vessel).ThenInclude(v => v!.LegalEntity).FirstOrDefault(a => a.Id == groupId);
                if (g != null)
                {
                    ViewBag.Property = $"{g.Property.Name} - {g.Property.LegalEntity.Name}";
                    if (g.VesselId.HasValue)
                    {
                        ViewBag.Vessel = $"{g.Vessel?.Name}, {g.Vessel?.Registration}, {g.Vessel?.OwnerName}";
                        //ViewBag.NauticalLegalEntity = $"{g.Vessel?.LegalEntity?.Name}";
                        ViewBag.NauticalLegalEntityData = g.NauticalLegalEntityData;
                    }                    
                    ViewBag.ResidenceTaxAmount = g.ResTaxAmount ?? 0m;
                    ViewBag.ResidenceTaxFee = g.ResTaxFee ?? 0m;
                    ViewBag.ResidenceTaxTotal = (g.ResTaxAmount ?? 0m) + (g.ResTaxFee ?? 0m);
                    ViewBag.PaymentDesc = g.ResTaxPaymentType != null ? g.ResTaxPaymentType.Description : string.Empty;
                }
                return PartialView();
            }
            catch (Exception ex)
            {
                // Handle exceptions and return error information
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult DeleteGroup(int groupId)
        {
            try
            {
                // TODO: Implement your logic to delete the group with the specified ID
                var group = _db.Groups.Include(x => x.Persons).SingleOrDefault(g => g.Id == groupId);
                  
                if (group.Persons.Any())
                {
                    return Json(new { error = "Grupa sadrži prijave i ne može se obrisati." });
                }

                if (group != null)
                {
                    _db.Groups.Remove(group);
                    _db.SaveChanges();
                    return Json(new { info = "Grupa uspješno obrisana." });
                }
                else
                {
                    return Json(new { error = "Grupa nije pronađena." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja grupe." });
            }
        }

        public ActionResult delgrp(int id)
		{
			var m = _db.Groups.SingleOrDefault(a => a.Id == id);

			if (_db.MnePersons.Any(a => a.GroupId == m.Id))
			{
				return Json(new { error = "Ne možete obrisati prijavu dok ne obrišete sve goste iz nje.", info = "" });
			}
			else
			{
				_db.Groups.Remove(m);
				_db.SaveChanges();
				return Json(new { error = "", info = "Uspješno obrisana prijava" });
			}
		}

		public ActionResult mup(int g, string w)
		{
			ViewBag.Grupa = g;
			ViewBag.What = w;
			return PartialView();
		}


		public async Task<ActionResult> RegisterGroup(int groupId)
		{
			var group = _db.Groups.Include(a => a.Property).ThenInclude(a => a.LegalEntity).SingleOrDefault(a => a.Id == groupId);			
			await _registerClient.RegisterGroup(group, null, null);
			return PartialView();
		}


		public ActionResult Send2Mup(int? id, int? g, string w, string p, string o, string _connectionID, bool retry = false)
		{
			//var result = _rb90client.Send2Mup(g.Value, null, null, retry);

			return null;
		}


        /*
		public async Task<ActionResult> certPrint(int id)
		{	
			var group = _db.Groups.First(a => a.Id == id);
			var stream = await _rb90client.ConfirmationPdf(group);
			stream.Seek(0, SeekOrigin.Begin);

			return File(stream, "application/pdf");
		}
		*/


        #region Guest Self Register

        [HttpPost]
        [Route("guestURL")]
        public async Task<ActionResult> GuestURL(int propertyId, int? unitId, string email, string phoneNo, string lang)
        {
            try
            {
                await _registerClient.SendGuestToken(propertyId, unitId, email, phoneNo, lang);
            }
            catch (Exception ex)
            {
                return Json(new { info = "", error = "Greška prilikom slanja linka za self register" });
            }
            return Json(new { info = "Uspješno poslat link za self register", error = "" });
        }

        [HttpPost]
        [Route("setLang")]
        public ActionResult lang(string l, string g)
        {
            HttpContext.Session.SetString("LANG", l);
            return RedirectToAction("register", new { g });
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("selfRegister")]
        public ActionResult selfRegister(string t, string g)
        {
            var objekat = 0;
            var jedinica = 0;
            var guid = "";
            var lang = "ENG";
            var expired = false;
            SelfRegisterToken token = null;

            if (t != null)
            {
				/*
				var result = _rb90GuestToken.IsTokenValid(t);

                objekat = result.Item1;
                jedinica = result.Item2;
                lang = result.Item3;
                guid = result.Item4;
                expired = result.Item5;
                HttpContext.Session.SetString("LANG", lang);
                token = _db.GuestTokens.Where(a => a.GUID == guid).FirstOrDefault()!;
				*/
            }
            else
            {
                token = _db.SelfRegisterTokens.Where(a => a.Guid == g).FirstOrDefault()!;
                objekat = token.PropertyId ?? 0;
                jedinica = token.PropertyUnitId ?? 0;
                guid = g;
                expired = false;
            }

            var property = _db.Properties.Where(a => a.Id == objekat).FirstOrDefault();

            if (objekat == 0)
            {
                ViewBag.Status = "TOKEN_INVALID";
            }
            else if (expired == true)
            {
                ViewBag.Status = "TOKEN_EXPIRED";
            }
            else
            {
                ViewBag.Status = "TOKEN_VALID";

                var grupa = _db.Groups.Where(a => a.Guid == guid).FirstOrDefault();

                if (grupa == null)
                {

                    grupa = new Group();
                    _db.Groups.Add(grupa);
                    grupa.Guid = guid;
                    grupa.Date = DateTime.Now;
                    grupa.PropertyId = objekat;
                    grupa.UnitId = jedinica;
                    grupa.Status = "DRAFT";
                    grupa.Email = token?.Email!;
                    grupa.LegalEntityId = property!.LegalEntityId;
                    _db.SaveChanges();
                }

                ViewBag.Grupa = grupa;
                ViewBag.Guid = grupa.Guid;
                ViewBag.Prijave = _db.MnePersons.Where(a => a.GroupId == grupa.Id).ToList();
            }

            ViewBag.Objekat = objekat;
            ViewBag.ObjekatDSC =
            ViewBag.Jedinica = jedinica;
            ViewBag.GUID = guid;
            ViewBag.Expired = expired;
            ViewBag.Expires = token!.Expires;
            ViewBag.Lang = HttpContext.Session.GetString("LANG") ?? "ENG";

            return View();
        }

        [HttpGet]
        [Route("selfRegisterGuests")]
        public ActionResult SelfRegisterGuests(string guid)
        {
            var grupa = _db.Groups.Where(a => a.Guid == guid).FirstOrDefault();

            if (grupa == null)
            {
                var ticket = _db.SelfRegisterTokens.Where(a => a.Guid == guid).FirstOrDefault();

                grupa.Guid = guid;
                grupa.Date = DateTime.Now;
                grupa.PropertyId = ticket!.PropertyId!.Value;
                grupa.UnitId = ticket.PropertyUnitId;
                grupa.Status = "DRAFT";
                grupa.Email = ticket?.Email!;
                grupa.LegalEntityId = 0;
                _db.Groups.Add(grupa);
                _db.SaveChanges();
            }

            ViewBag.Grupa = grupa;
            ViewBag.GUID = guid;
            ViewBag.Prijave = _db.MnePersons.Where(a => a.GroupId == grupa.Id).ToList();
            ViewBag.Lang = HttpContext.Session.GetString("LANG") ?? "ENG";

            return View();
        }


        [HttpPost]
        [Route("selfRegisterGuest")]
        public ActionResult SelfRegisterGuest(int g, int? id)
        {
            var grupa = _db.Groups.Where(a => a.Id == g).FirstOrDefault();
            var p = _db.MnePersons.FirstOrDefault(a => a.Id == id);

            if (id == 0)
            {
                p = new MnePerson();
            }

            ViewBag.Prijava = p;
            ViewBag.Lang = HttpContext.Session.GetString("LANG") ?? "ENG";

            return View();
        }


        #endregion
    }
}
