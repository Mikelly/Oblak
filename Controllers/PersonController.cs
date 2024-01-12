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
		private readonly int _legalEntityId;
		

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
                _legalEntityId = _appUser.LegalEntityId;
                if (_appUser.LegalEntity.Country == Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
            }
        }
		

		[HttpGet]
        [Route("groupPersons", Name = "GroupPersons")]
        public async Task<ActionResult> GroupPersons(int groupId)
        {
            var group = await _db.Groups.Where(x => x.Id == groupId).FirstOrDefaultAsync();
            ViewBag.GroupId = groupId;
            ViewBag.PropertyId = group!.PropertyId;
            ViewBag.CheckIn = group!.CheckIn;
            ViewBag.CheckOut = group!.CheckOut;

            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .ToListAsync();

            if (_appUser.LegalEntity.Country == Country.SRB)
            {
                codeLists = codeLists.Where(a => a.Country == "SRB").ToList();

                var srbViewModel = new PersonViewModel
                {
                    GenderCodeList = codeLists.Where(x => x.Type == "Gender").ToList(),
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

            if (_appUser.LegalEntity.Country == Country.MNE)
            {
                codeLists = codeLists.Where(a => a.Country == "SRB").ToList();

                var model = new PersonViewModel
                {
                    GenderCodeList = codeLists.Where(x => x.Type == "pol").ToList(),
                    CountryCodeList = codeLists.Where(x => x.Type == "drzava").ToList(),
                    DocumentTypeCodeList = codeLists.Where(x => x.Type == "isprava").ToList(),
                    EntryPointCodeList = codeLists.Where(x => x.Type == "prelaz").ToList(),
                    PersonTypeCodeList = codeLists.Where(x => x.Type == "gost").ToList(),
                    VisaTypeCodeList = codeLists.Where(x => x.Type == "viza").ToList(),
                };

                return View("MnePersons", model);
            }

            return View();
        }

		[HttpGet]
		[Route("persons", Name = "Persons")]
		public async Task<ActionResult> Persons()
		{
			var isPropertyAdmin = User.IsInRole("PropertyAdmin");
			var legalEntityId = _appUser!.LegalEntityId;

			List<PropertyDto> properties = null;

			if (isPropertyAdmin)
			{
				var ids = _db.LegalEntities.Where(a => a.AdministratorId == legalEntityId).Select(a => a.Id).ToList();
				properties = _db.Properties.Where(a => ids.Contains(a.LegalEntityId)).ToList()
					.Select(a => _mapper.Map<Property, PropertyDto>(a)).ToList();
			}
			else
			{
				properties = _db.Properties.Where(a => a.LegalEntityId == legalEntityId).ToList()
					.Select(a => _mapper.Map<Property, PropertyDto>(a)).ToList();
			}

            ViewBag.Properties = properties;

            if (_appUser.LegalEntity.Country == Country.SRB)
            {
                return View("SrbPersons");
            }
            else if(_appUser.LegalEntity.Country == Country.MNE)
            {
                return View("MnePersons");
            }
            return View("");
		}

        public async Task<ActionResult> Get(int? person)
        {
            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .ToListAsync();

            if (_appUser.LegalEntity.Country == Country.SRB)
            {
                SrbPersonEnrichedDto dto = null;
                if (person == null) dto = new SrbPersonEnrichedDto();
                else dto = dto;

                var srbViewModel = new PersonViewModel
                {
                    GenderCodeList = codeLists.Where(x => x.Type == "Gender").ToList(),
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

                ViewBag.Dto = dto;
                return PartialView("SrbPerson", srbViewModel);
            }

            if (_appUser.LegalEntity.Country == Country.MNE)
            {
                MnePersonEnrichedDto dto = null;
                if (person == null) dto = new MnePersonEnrichedDto();
                else dto = dto;

                codeLists = codeLists.Where(a => a.Country == "MNE").ToList();

                var model = new PersonViewModel
                {
                    GenderCodeList = codeLists.Where(x => x.Type == "pol").ToList(),
                    CountryCodeList = codeLists.Where(x => x.Type == "drzava").ToList(),
                    DocumentTypeCodeList = codeLists.Where(x => x.Type == "isprava").ToList(),
                    EntryPointCodeList = codeLists.Where(x => x.Type == "prelaz").ToList(),
                    PersonTypeCodeList = codeLists.Where(x => x.Type == "gost").ToList(),
                    VisaTypeCodeList = codeLists.Where(x => x.Type == "viza").ToList(),
                };

                ViewBag.Dto = dto;
                return PartialView("MnePerson", model);
            }

            return PartialView();
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
                .Where(a => a.LegalEntityId == _legalEntityId)
                .Where(x => x.GroupId == groupId)
                .Include(a => a.Property)
                .OrderByDescending(x => x.Id)
                .Select(a => new MnePersonEnrichedDto
                {
                    Id = a.Id,
                    PropertyId = a.PropertyId,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    FullName = a.FirstName + ' ' + a.LastName,
                    Gender = a.Gender,
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
                var property = _db.Properties.FirstOrDefault(a => a.Id == newGuestDto.PropertyId);
                newGuest.LegalEntityId = property.LegalEntityId;

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

        public async Task<ActionResult> ReadSrbPersons([DataSourceRequest] DataSourceRequest request, int groupId)
        {
            var reqCodeLists = new List<string>()
            {
                "DocumentType",
            };

            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .Where(a => reqCodeLists.Contains(a.Type))
                .ToListAsync();

            var countryDictionary = codeLists
                .Where(x => x.Type == "Country")
                .ToDictionary(x => x.Param2.ToString(), x => x.ExternalId);
            var documentTypeDictionary = codeLists
                .Where(x => x.Type == "DocumentType")
                .ToDictionary(x => x.ExternalId, x => x.Name);
            var entryPlaceDictionary = codeLists
                .Where(x => x.Type == "EntryPlace")
                .ToDictionary(x => x.ExternalId, x => x.Name);
            var visaTypeDictionary = codeLists
                .Where(x => x.Type == "VisaType")
                .ToDictionary(x => x.ExternalId, x => x.Name);
            var serviceTypeDictionary = codeLists
                .Where(x => x.Type == "ServiceType")
                .ToDictionary(x => x.ExternalId, x => x.Name);
            var arrivalTypeDictionary = codeLists
                .Where(x => x.Type == "ArrivalType")
                .ToDictionary(x => x.ExternalId, x => x.Name);
            var reasonForStayDictionary = codeLists
                .Where(x => x.Type == "ReasonForStay")
                .ToDictionary(x => x.ExternalId, x => x.Name);
            var discountReasonDictionary = codeLists
                .Where(x => x.Type == "ResidenceTaxDiscountReason")
                .ToDictionary(x => x.ExternalId, x => x.Name);

            var data = _db.SrbPersons
                .Where(a => a.LegalEntityId == _legalEntityId)
                .Where(x => x.GroupId == groupId || groupId == 0)
                .Include(a => a.Property)
                .OrderByDescending(x => x.Id)
                .Select(a => new SrbPersonEnrichedDto
                {
                    Id = a.Id,
                    PropertyId = a.PropertyId,
                    IsDomestic = a.IsDomestic,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    FullName = a.FirstName + ' ' + a.LastName,
                    Gender = a.Gender,
                    PropertyName = a.Property.PropertyName,
                    BirthDate = a.BirthDate,
                    PersonalNumber = a.PersonalNumber,
                    BirthPlaceName = a.BirthPlaceName,
                    BirthCountryIso2 = a.BirthCountryIso2,
                    BirthCountryIso3 = a.BirthCountryIso3,
                    BirthCountryExternalId = a.BirthCountryIso3 != null ? countryDictionary.GetValueOrNull(a.BirthCountryIso3) : null,
                    NationalityIso2 = a.NationalityIso2,
                    NationalityIso3 = a.NationalityIso3,
                    NationalityExternalId = a.NationalityIso3 != null ? countryDictionary.GetValueOrNull(a.NationalityIso3) : null,
                    ResidenceCountryIso2 = a.ResidenceCountryIso2,
                    ResidenceCountryIso3 = a.ResidenceCountryIso3,
                    ResidenceCountryExternalId = a.ResidenceCountryIso3 != null ? countryDictionary.GetValueOrNull(a.ResidenceCountryIso3) : null,
                    ResidenceMunicipalityCode = a.ResidenceMunicipalityCode,
                    ResidenceMunicipalityName = a.ResidenceMunicipalityName,
                    ResidencePlaceCode = a.ResidencePlaceCode,
                    ResidencePlaceName = a.ResidencePlaceName,
                    CheckIn = a.CheckIn,
                    DocumentType = a.DocumentType != null ? documentTypeDictionary.GetValueOrNull(a.DocumentType) : null,
                    DocumentNumber = a.DocumentNumber,
                    DocumentIssueDate = a.DocumentIssueDate,
                    EntryDate = a.EntryDate,
                    EntryPlace = a.EntryPlace,
                    EntryPlaceCode = a.EntryPlaceCode,
                    VisaType = a.VisaType != null ? visaTypeDictionary.GetValueOrNull(a.VisaType) : null,
                    VisaNumber = a.VisaNumber,
                    VisaIssuingPlace = a.VisaIssuingPlace,
                    StayValidTo = a.StayValidTo,
                    Note = a.Note,
                    ServiceType = a.ServiceType != null ? serviceTypeDictionary.GetValueOrNull(a.ServiceType) : null,
                    ArrivalType = a.ArrivalType != null ? serviceTypeDictionary.GetValueOrNull(a.ArrivalType) : null,
                    ReasonForStay = a.ReasonForStay != null ? reasonForStayDictionary.GetValueOrNull(a.ReasonForStay) : null,
                    PlannedCheckOut = a.PlannedCheckOut,
                    ResidenceTaxDiscountReason = a.ResidenceTaxDiscountReason != null ? discountReasonDictionary.GetValueOrNull(a.ResidenceTaxDiscountReason) : null
                });

            return Json(await data.ToDataSourceResultAsync(request));
        }

        [HttpPost]
        public async Task<ActionResult> CreateSrbPerson(SrbPersonDto newGuestDto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var newGuest = _mapper.Map<SrbPersonDto, SrbPerson>(newGuestDto);
                newGuest.LegalEntityId = _legalEntityId;

                var validation = _registerClient.Validate(newGuest, newGuest.CheckIn, null);

                if (validation.ValidationErrors.Any())
                {
                    return Json(new { success = false, errors = validation.ValidationErrors });
                }

                await _db.SrbPersons.AddAsync(newGuest);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult DeleteSrbPerson(int guestId)
        {
            try
            {
                var guest = _db.SrbPersons.Find(guestId);

                if (guest != null)
                {
                    _db.SrbPersons.Remove(guest);
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

        public async Task<ActionResult> GetSrbMunicipalityList()
        {
            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .Where(x => x.Type == "ResidenceMunicipality")
                .ToListAsync();

            return Json(codeLists);
        }

        public async Task<ActionResult> GetSrbPlaceList(string municipalityId)
        {
            var municipality = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .Where(x => x.Type == "ResidenceMunicipality")
                .Where(a => a.ExternalId == municipalityId)
                .Select(x => x.Param1)
                .FirstOrDefaultAsync();

            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .Where(x => x.Type == "Place")
                .Where(x => x.Param1 == municipality)
                .ToListAsync();

            return Json(codeLists);
        }
    }
}
