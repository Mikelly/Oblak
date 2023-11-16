using AutoMapper;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Interfaces;
using Oblak.Models.rb90;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Telerik.Windows.Documents.Flow.Model;

namespace RegBor.Controllers
{
	public class GroupController : Controller
	{
        private readonly Register _registerClient;
        private readonly MneClient _rb90client;
		private readonly ApplicationDbContext _db;
		private readonly ILogger<GroupController> _logger;
		private readonly IMapper _mapper;
		private readonly ApplicationUser _appUser;
		private readonly int _company;
		

		public GroupController(
			IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            MneClient rb90Client,
			ApplicationDbContext db,
			IMapper mapper,
			ILogger<GroupController> logger
			)
		{
			_db = db;
			_rb90client = rb90Client;
			_logger = logger;
			_mapper = mapper;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
				var _company = _appUser?.LegalEntityId;
                if (_appUser.LegalEntity.Country == Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
            }
        }

		[HttpGet]
		[Route("groups", Name = "Groups")]
		public ActionResult Index()
		{			
			return View();
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

		public ActionResult NovaGrupa(int objekat, int? jedinica, string dolazak, string odlazak, string email)
		{
			var g = _db.Groups.Where(a => a.PropertyId == objekat)
				.Where(a => a.UnitId == jedinica || jedinica == null)
				.Where(a => a.CheckIn == null && a.CheckOut == null && _db.MnePersons.Where(b => b.GroupId == a.Id).Any() == false)
				.OrderByDescending(a => a.Date).FirstOrDefault();

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

			g.LegalEntityId = _company;
			g.PropertyId = objekat;
			g.UnitId = jedinica;
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
			var jedinice = _db.PropertyUnits.Where(a => a.LegalEntityId == _company).ToList();

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

		public ActionResult fsetv([Bind(Prefix = "p")] rb_PrijavaVM vm, string name)
		{
			var m = _db.MnePersons.SingleOrDefault(a => a.Id == vm.ID);

			name = name.Replace("p.", "").ToLower();

			if (name == "jmbg")
			{
				m.PersonalNumber = vm.JMBG;
			}
			else if (name == "objekatid")
			{
				m.PropertyId = vm.ObjekatID;
				//m.Naziv = _db.rb_Objekti.Where(a => a.RefID == m.ObjekatID).Select(a => a.Naziv).DefaultIfEmpty("").FirstOrDefault();
			}
			else if (name == "tip")
			{
				m.PersonType = vm.Tip;
			}
			else if (name == "naziv")
			{
				//m.Naziv = vm.Naziv;
			}
			else if (name == "prezime")
			{
				m.LastName = vm.Prezime;
			}
			else if (name == "ime")
			{
				m.FirstName = vm.Ime;
			}
			else if (name == "pol")
			{
				m.Gender = vm.Pol;
			}
			else if (name == "drzava")
			{
				m.Nationality = vm.Drzava;
				if (string.IsNullOrEmpty(m.DocumentCountry)) m.DocumentCountry = vm.Drzava;
				if (string.IsNullOrEmpty(m.BirthCountry)) m.BirthCountry = vm.Drzava;
				if (string.IsNullOrEmpty(m.PermanentResidenceCountry)) m.PermanentResidenceCountry = vm.Drzava;
			}
			else if (name == "rodj_datum")
			{
				m.BirthDate = vm.Rodj_Datum;
			}
			else if (name == "rodj_mjesto")
			{
				m.BirthPlace = vm.Rodj_Mjesto;
			}
			else if (name == "rodj_drzava")
			{
				m.BirthCountry = vm.Rodj_Drzava;				
				if (string.IsNullOrEmpty(m.Nationality)) m.Nationality = vm.Drzava;
				if (string.IsNullOrEmpty(m.DocumentCountry)) m.DocumentCountry = vm.Drzava;				
				if (string.IsNullOrEmpty(m.PermanentResidenceCountry)) m.PermanentResidenceCountry = vm.Drzava;
			}
			else if (name == "preb_adresa")
			{
				m.PermanentResidenceAddress = vm.Preb_Adresa;
			}
			else if (name == "preb_mjesto")
			{
				m.PermanentResidencePlace = vm.Preb_Mjesto;
			}
			else if (name == "preb_drzava")
			{
				m.PermanentResidenceCountry = vm.Preb_Drzava;
				if (string.IsNullOrEmpty(m.Nationality)) m.Nationality = vm.Drzava;
				if (string.IsNullOrEmpty(m.DocumentCountry)) m.DocumentCountry = vm.Drzava;
				if (string.IsNullOrEmpty(m.BirthCountry)) m.BirthCountry = vm.Drzava;
			}
			else if (name == "ld_vrsta")
			{
				m.DocumentType = vm.LD_Vrsta;
			}
			else if (name == "ld_broj")
			{
				m.DocumentNumber = vm.LD_Broj;
			}
			else if (name == "ld_rok")
			{
				m.DocumentValidTo = vm.LD_Rok;
			}
			else if (name == "ld_drzava")
			{
				m.DocumentCountry = vm.LD_Drzava;
				if (string.IsNullOrEmpty(m.Nationality)) m.Nationality = vm.Drzava;
				if (string.IsNullOrEmpty(m.PermanentResidenceCountry)) m.PermanentResidenceCountry = vm.Drzava;
				if (string.IsNullOrEmpty(m.BirthCountry)) m.BirthCountry = vm.Drzava;
			}
			else if (name == "ld_organ")
			{
				m.DocumentIssuer = vm.LD_Organ;
			}
			else if (name == "borav_prijava")
			{
				m.CheckIn = vm.Borav_Prijava;
			}
			else if (name == "borav_odjava")
			{
				m.CheckOut = vm.Borav_Odjava;
			}
			else if (name == "ulaz_mjesto")
			{
				m.EntryPoint = vm.Ulaz_Mjesto;
			}
			else if (name == "ulaz_datum")
			{
				m.EntryPointDate = vm.Ulaz_Datum;
			}
			else if (name == "visa_vrsta")
			{
				m.VisaType = vm.Visa_Vrsta;
			}
			else if (name == "visa_broj")
			{
				m.VisaNumber = vm.Visa_Broj;
			}
			else if (name == "visa_mjesto")
			{
				m.VisaIssuePlace = vm.Visa_Mjesto;
			}
			else if (name == "visa_od")
			{
				m.VisaValidFrom = vm.Visa_Od;
			}
			else if (name == "visa_do")
			{
				m.VisaValidTo = vm.Visa_Do;
			}

			try
			{
				_db.SaveChanges();
			}
			catch (Exception e)
			{

			}

			return Json(new { error = "", js = "" });
		}

		public ActionResult fsetall([Bind(Prefix = "p")] rb_PrijavaVM vm)
		{
			var m = _db.MnePersons.SingleOrDefault(a => a.Id == vm.ID);
			var frm = m.LegalEntityId;
			var grp = m.GroupId;

			_mapper.Map(vm, m);

			m.LegalEntityId = frm;
			m.PropertyId = vm.ObjekatID;
			m.GroupId = grp;
			m.Status = "A";
			
			try
			{
				_db.SaveChanges();
			}
			catch (Exception e)
			{
				string errors = "";
				var validationErrors = _db.ChangeTracker
					.Entries<IValidatableObject>()
					.SelectMany(e => e.Entity.Validate(null))
					.Where(r => r != ValidationResult.Success);
				/*
				if (validationErrors.Any())
				{
					foreach (var eve in validationErrors)
					{
						errors += String.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:", eve.Entity.GetType().Name, eve.Entry.State);
						foreach (var ve in eve.ValidationErrors)
						{
							errors += String.Format("- Property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage);
						}
					}
					return Json(new { error = errors, js = "" });
				}
				else
				{
					return Json(new { error = Helpers.StringException(e), js = "" });
				}
				*/
			}

			return Json(new { error = "", js = "" });
		}

		public virtual ActionResult Read([DataSourceRequest] DataSourceRequest request)
		{
			var data = _db.Groups.Where(a => a.LegalEntityId == _company).Select(a => new rb_GrupaVM { });

			return Json(data.ToDataSourceResult(request));
			
		}

		public ActionResult Create([DataSourceRequest] DataSourceRequest request, rb_GrupaVM vm)
		{
			var m = new Group();
			_mapper.Map(vm, m);

			if (ModelState.IsValid)
			{
				_db.Groups.Add(m);
				_db.SaveChanges();
			}

			return Json(new[] { _mapper.Map(m, vm) }.ToDataSourceResult(request, ModelState));
		}

		public ActionResult Update([DataSourceRequest] DataSourceRequest request, rb_GrupaVM vm)
		{
			var m = _db.Groups.SingleOrDefault(a => a.Id == vm.ID);
			_mapper.Map(vm, m);

			if (ModelState.IsValid)
			{
				_db.SaveChanges();
			}

			return Json(new[] { _mapper.Map(m, vm) }.ToDataSourceResult(request, ModelState));
		}

		public ActionResult Destroy([DataSourceRequest] DataSourceRequest request, rb_GrupaVM vm)
		{
			var m = _db.Groups.SingleOrDefault(a => a.Id == vm.ID);

			if (ModelState.IsValid)
			{
				_db.Groups.Remove(m);
				_db.SaveChanges();
			}

			return Json(new[] { vm }.ToDataSourceResult(request, ModelState));
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
