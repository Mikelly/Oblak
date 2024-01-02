using AutoMapper;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace RegBor.Controllers
{
	public class PersonController : Controller
	{
        private readonly Register _registerClient;        
		private readonly ApplicationDbContext _db;
		private readonly ILogger<PersonController> _logger;
		private readonly IMapper _mapper;
		private readonly ApplicationUser _appUser;
		private readonly int _company;
		

		public PersonController(
			IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,            
			ApplicationDbContext db,
			IMapper mapper,
			ILogger<PersonController> logger
			)
		{
			_db = db;			
			_logger = logger;
			_mapper = mapper;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
                _company = _appUser.LegalEntityId;
                if (_appUser.LegalEntity.Country == Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
            }
        }
		

		[HttpGet]
        [Route("persons", Name = "Persons")]
        public async Task<ActionResult> Persons(int groupId)
        {
            var group = await _db.Groups.Where(x => x.Id == groupId).FirstOrDefaultAsync();
            ViewBag.GroupId = groupId;
            ViewBag.PropertyId = group!.PropertyId;
            ViewBag.CheckIn = group!.CheckIn;
            ViewBag.CheckOut = group!.CheckOut;

            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .ToListAsync();

            var model = new PersonViewModel
            {
                CountryCodeList = codeLists.Where(x => x.Type == "drzava").ToList(),
                DocumentTypeCodeList = codeLists.Where(x => x.Type == "isprava").ToList(),
                EntryPointCodeList = codeLists.Where(x => x.Type == "prelaz").ToList(),
                PersonTypeCodeList = codeLists.Where(x => x.Type == "gost").ToList(),
                VisaTypeCodeList = codeLists.Where(x => x.Type == "viza").ToList(),
            };

            if (_appUser.LegalEntity.Country == Country.SRB)
            {
                return View("SrbPersons", model);
            }
            return View("MnePersons", model);
        }

		[HttpGet]
		[Route("persons", Name = "Persons")]
		public async Task<ActionResult> Persons()
		{
			var codeLists = await _db.CodeLists
				.Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
				.ToListAsync();

			if (_appUser.LegalEntity.Country == Country.SRB)
			{
				var srbViewModel = new PersonViewModel
				{
					CountryCodeList = codeLists.Where(x => x.Type == "Country").ToList(),
					DocumentTypeCodeList = codeLists.Where(x => x.Type == "DocumentType").ToList(),
					MunicipalityCodeList = codeLists.Where(x => x.Type == "ResidenceMunicipality").ToList(),
					PlaceCodeList = codeLists.Where(x => x.Type == "Place").ToList(),
					VisaTypeCodeList = codeLists.Where(x => x.Type == "VisaType").ToList(),
					ServiceTypeCodeList = codeLists.Where(x => x.Type == "ServiceType").ToList(),
					ArrivalTypeCodeList = codeLists.Where(x => x.Type == "ArrivalType").ToList(),
					ReasonForStayCodeList = codeLists.Where(x => x.Type == "ReasonForStay").ToList(),
					EntryPointCodeList = codeLists.Where(x => x.Type == "EntryPlace").ToList(),
					DiscountReasonCodeList = codeLists.Where(x => x.Type == "ResidenceTaxDiscountReason").ToList(),
				};

				return View("SrbPersons", srbViewModel);
			}

			var model = new PersonViewModel
			{
				CountryCodeList = codeLists.Where(x => x.Type == "drzava").ToList(),
				DocumentTypeCodeList = codeLists.Where(x => x.Type == "isprava").ToList(),
				EntryPointCodeList = codeLists.Where(x => x.Type == "prelaz").ToList(),
				PersonTypeCodeList = codeLists.Where(x => x.Type == "gost").ToList(),
				VisaTypeCodeList = codeLists.Where(x => x.Type == "viza").ToList(),
			};

			return View("MnePersons", model);
		}

		public async Task<ActionResult> ReadMnePersons([DataSourceRequest] DataSourceRequest request, int groupId)
        {
            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .Where(a => a.Type == "gost" || a.Type == "isprava")
                .ToListAsync();

            var personTypeDictionary = codeLists
                .Where(x => x.Type == "gost")
                .ToDictionary(x => x.ExternalId, x => x.Name);

            var documentTypeDictionary = codeLists
                .Where(x => x.Type == "isprava")
                .ToDictionary(x => x.ExternalId, x => x.Name);

            var data = _db.MnePersons
                .Where(a => a.LegalEntityId == _company)
                .Where(x => x.GroupId == groupId)
                .Include(a => a.Property)
                .OrderByDescending(x => x.UserCreatedDate)
                .Select(a => new MnePersonEnrichedDto
                {
                    Id = a.Id,
                    PropertyId = a.PropertyId,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    FullName = a.FirstName + ' ' + a.LastName,
                    PropertyName = a.Property.PropertyName,
                    PersonType = personTypeDictionary.GetValueOrNull(a.PersonType),
                    PersonalNumber = a.PersonalNumber,
                    BirthDate = a.BirthDate,
                    BirthPlace = a.BirthPlace,
                    BirthCountry = a.BirthCountry,
                    Nationality = a.Nationality,
                    PermanentResidencePlace = a.PermanentResidencePlace,
                    PermanentResidenceAddress = a.PermanentResidenceAddress,
                    PermanentResidenceCountry = a.PermanentResidenceCountry,
                    CheckIn = a.CheckIn,
                    CheckOut = a.CheckOut,
                    DocumentType = documentTypeDictionary.GetValueOrNull(a.DocumentType),
                    DocumentNumber = a.DocumentNumber,
                    DocumentValidTo = a.DocumentValidTo,
                    DocumentIssuer = a.DocumentIssuer,
                    EntryPoint = a.EntryPoint,
                    EntryPointDate = a.EntryPointDate,
                    VisaType = a.VisaType,
                    VisaNumber = a.VisaNumber,
                    VisaIssuePlace = a.VisaIssuePlace,
                    VisaValidFrom = a.VisaValidFrom,
                    VisaValidTo = a.VisaValidTo
                });

            return Json(await data.ToDataSourceResultAsync(request));
        }

        [HttpPost]
        public async Task<ActionResult> CreateMnePerson(MnePersonDto newGuestDto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var newGuest = _mapper.Map<MnePersonDto, MnePerson>(newGuestDto);
                newGuest.LegalEntityId = _company;

                var validation = _registerClient.Validate(newGuest, newGuest.CheckIn, newGuest.CheckOut);

                if (validation.ValidationErrors.Any())
                {
                    return Json(new { success = false, errors = validation.ValidationErrors });
                }

                await _db.MnePersons.AddAsync(newGuest);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult DeleteMnePerson(int guestId)
        {
            try
            {
                var guest = _db.MnePersons.Find(guestId);

                if (guest != null)
                {
                    _db.MnePersons.Remove(guest);
                    _db.SaveChanges();
                    return Json(new { info = "Gost uspješno obrisan." });
                }
                else
                {
                    return Json(new { error = "Gost nije pronađen." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja gosta." });
            }
        }


        public virtual ActionResult ReadSrbPersons([DataSourceRequest] DataSourceRequest request)
        {
            var data = _db.Groups
                .Where(a => a.LegalEntityId == _company)
                .Include(a => a.Property)
                .OrderByDescending(x => x.Date)
                .Select(a => new GroupEnrichedDto
                {
                    Id = a.Id,
                    //Date = a.Date,
                    //PropertyName = a.Property.PropertyName,
                    //CheckIn = a.CheckIn,
                    //CheckOut = a.CheckOut,
                    //Email = a.Email,
                    //Guests = a.Persons.Any() ? $"{a.Persons.Count}: {string.Join(", ", a.Persons.Select(p => $"{p.FirstName} {p.LastName}"))}" : ""
                });

            return Json(data.ToDataSourceResult(request));
        }
    }
}
