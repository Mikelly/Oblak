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
using Oblak.Services.Reporting;
using Oblak.Services.SRB;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Oblak.Controllers
{
    public class PersonController : Controller
    {
        private readonly Register _registerClient;
        private readonly ReportingService _reporting;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PersonController> _logger;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly IWebHostEnvironment _env;
        private readonly int _legalEntityId;


        public PersonController(
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext db,
            IMapper mapper,
            ILogger<PersonController> logger,
            ReportingService reporting,
            IWebHostEnvironment env
            )
        {
            _db = db;
            _logger = logger;
            _mapper = mapper;
            _reporting = reporting;
            _env = env;

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

                var srbViewModel = new CodeListViewModel
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
                codeLists = codeLists.Where(a => a.Country == "MNE").ToList();
                var model = new CodeListViewModel
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
        public async Task<ActionResult> Persons(int? groupId)
        {
            var group = await _db.Groups.Where(x => x.Id == groupId).FirstOrDefaultAsync();
            ViewBag.Group = groupId;

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

            else if (_appUser.LegalEntity.Country == Country.MNE)
            {
                return View("MnePersons");
            }

            return View("");
        }

        [HttpPost]
        [Route("mrz")]
        public async Task<ActionResult> Mrz(int? group, [FromBody] MrzDto mrz)
        {
            if (_appUser.LegalEntity.Country == Country.SRB)
            {

            }
            else if (_appUser.LegalEntity.Country == Country.MNE)
            {

            }

            return Ok();
        }

        public async Task<ActionResult> Get(int? person, int? group)
        {
            MrzDto mrz = null;
            try
            {
                var m = Request.Form["mrz"];
                mrz = JsonSerializer.Deserialize<MrzDto>(m);
            }
            catch (Exception ex) { }

            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .ToListAsync();

            ViewBag.Group = group;

            if (_appUser.LegalEntity.Country == Country.SRB)
            {
                SrbPersonEnrichedDto dto = null;
                if (person == null) dto = new SrbPersonEnrichedDto();
                else dto = dto;

                var srbViewModel = new CodeListViewModel
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
                if (person == 0)
                {
                    dto = new MnePersonEnrichedDto();
                    if (group.HasValue && group != 0)
                    {
                        var g = _db.Groups.Include(a => a.Property).FirstOrDefault(a => a.Id == group);
                        if (g != null)
                        {
                            dto.PropertyId = g.Property.Id;
                            dto.PropertyName = g.Property.Name;
                            dto.CheckIn = g.CheckIn.Value;
                            dto.CheckOut = g.CheckOut.Value;
                        }
                    }
                    else
                    {
                        if (mrz != null)
                        {
                            dto.PersonType = mrz.DocIssuer == "MNE" ? "1" : "4";
                            dto.LastName = mrz.HolderNamePrimary;
                            dto.FirstName = mrz.HolderNameSecondary;
                            dto.Nationality = mrz.HolderNationality;
                            dto.BirthDate = mrz.HolderDateOfBirthDate();
                            dto.Gender = mrz.HolderSex;
                            dto.PersonalNumber = mrz.HolderNumber;
                            dto.DocumentCountry = mrz.DocIssuer;
                            dto.DocumentIssuer = mrz.DocAuthority;
                            dto.DocumentNumber = mrz.DocNumber;
                            dto.DocumentValidTo = mrz.DocExpiryDate();
                            dto.DocumentType = mrz.DocType == "IcaoTd1" || mrz.DocType == "IcaoTd2" ? "2" : "1";
                            dto.CheckIn = DateTime.Now;
                            dto.CheckOut = DateTime.Now.AddDays(1);
                            dto.BirthCountry = mrz.DocIssuer;
                            dto.PermanentResidenceCountry = mrz.DocIssuer;
                        }
                        else
                        {
                            dto.CheckIn = DateTime.Now;
                            dto.CheckOut = DateTime.Now.AddDays(1);
                        }

                        var legalEntityId = _appUser!.LegalEntityId;
                        var isPropertyAdmin = User.IsInRole("PropertyAdmin");
                        if (isPropertyAdmin == false)
                        {
                            var properties = _db.Properties.Where(a => a.LegalEntityId == legalEntityId).ToList();
                            if (properties.Count == 1)
                            {
                                dto.PropertyId = properties[0].Id;
                                dto.PropertyName = properties[0].Name;
                            }
                        }

                    }
                }
                else
                {
                    var p = _db.MnePersons.Include(a => a.Property).Include(a => a.LegalEntity).Include(a => a.Group).FirstOrDefault(a => a.Id == person);
                    dto = new MnePersonEnrichedDto();
                    dto = dto.GetFromMnePerson(p);
                }

                codeLists = codeLists.Where(a => a.Country == "MNE").ToList();

                var model = new CodeListViewModel
                {
                    GenderCodeList = codeLists.Where(x => x.Type == "pol").ToList(),
                    CountryCodeList = codeLists.Where(x => x.Type == "drzava").ToList(),
                    DocumentTypeCodeList = codeLists.Where(x => x.Type == "isprava").ToList(),
                    EntryPointCodeList = codeLists.Where(x => x.Type == "prelaz").ToList(),
                    PersonTypeCodeList = codeLists.Where(x => x.Type == "gost").ToList(),
                    VisaTypeCodeList = codeLists.Where(x => x.Type == "viza").ToList(),
                    ResTaxPaymentTypes = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId).ToDictionary(a => a.Id.ToString(), b => b.Description),
                    ResTaxTypes = _db.ResTaxTypes.Where(a => a.PartnerId == _appUser.PartnerId).ToDictionary(a => a.Id.ToString(), b => b.Description)

                    //ResTaxStatuses = new Dictionary<string, string>() { { "Unpaid", "Nije plaćena" }, { "Cash", "Plaćena gotovinom" }, { "Card", "Plaćena karticom" }, { "BankAccount", "Plaćena virmanski" } },
                    //ResTaxTypes = new Dictionary<string, string>() { { "Unpaid", "Nije plaćena" }, { "Cash", "Plaćena gotovinom" }, { "Card", "Plaćena karticom" }, { "BankAccount", "Plaćena virmanski" } },
                };

                ViewBag.Dto = dto;

                ViewBag.Disabled = false;
                ViewBag.TO = false;

                if (User.IsInRole("TouristOrg"))
                {
                    ViewBag.TO = true;
                    if (User.IsInRole("TouristOrgOperator"))
                    {
                        if (dto.ExternalId != null) ViewBag.Disabled = true;                        
                    }
                }
                return PartialView("MnePerson", model);
            }

            return PartialView();
        }

        public ActionResult ResTax(int property, int? resType, int? payType, string birthDate, string checkIn, string checkOut)
        {
            var p = _db.Properties.Include(a => a.LegalEntity).FirstOrDefault(a => a.Id == property);
            var pid = p.LegalEntity.PartnerId;
            var rt = _db.ResTaxTypes.FirstOrDefault(a => a.Id == resType);
            var pt = _db.ResTaxPaymentTypes.FirstOrDefault(a => a.Id == payType);

            DateTime? bd = null;
            DateTime? ci = null;
            DateTime? co = null;

            if (birthDate != null)
            {
                bd = DateTime.ParseExact(birthDate, "dd.MM.yyyy", null);
                if (rt != null && (rt.AgeFrom != null || rt.AgeTo != null))
                {
                    var zero = new DateTime(1, 1, 1);
                    var span = (DateTime.Now.Date - bd.Value.Date);
                    var age = (zero + span).Year - 1;

                    rt = _db.ResTaxTypes.Where(a => a.PartnerId == pid).FirstOrDefault(a => a.AgeFrom <= age && age <= a.AgeTo);
                }
            }

            int days = 0;
            decimal tax = 0;
            decimal fee = 0;

            if (checkIn != null && checkOut != null)
            {
                ci = DateTime.ParseExact(checkIn, "dd.MM.yyyy", null);
                co = DateTime.ParseExact(checkOut, "dd.MM.yyyy", null);

                days = (int)(co.Value.Date - ci.Value.Date).TotalDays;
                if (days < 0) days = 0;
            }

            if (rt != null)
            {
                tax = rt.Amount * days;
            }

            if (pt != null)
            {
                var resTaxFees = _db.ResTaxFees.Where(a => a.ResTaxPaymentTypeId == pt.Id).ToList();
                if (resTaxFees.Any())
                {
                    var resTaxFee = resTaxFees.Where(a => a.LowerLimit <= tax && tax <= a.UpperLimit).FirstOrDefault();
                    if (resTaxFee != null)
                    {
                        if (resTaxFee.FeeAmount.HasValue) fee = resTaxFee.FeeAmount.Value;
                        if (resTaxFee.FeePercentage.HasValue) fee = resTaxFee.FeePercentage.Value / 100m * tax;
                    }
                }
            }

            return Json(new { tax, fee, resType = rt?.Id, payType = pt?.Id });
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

            var query = _db.MnePersons.Include(a => a.Property)
                .Where(a => a.LegalEntityId == _legalEntityId);

            if (groupId != 0)
            {
                query = query.Where(a => a.GroupId == groupId);
            }
            else
            {
                query = query.Where(a => a.GroupId == null);
            }

            var data = query
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
                    VisaValidTo = a.VisaValidTo,
                    Registered = a.ExternalId != null,
                    Deleted = a.IsDeleted

                });

            return Json(await data.ToDataSourceResultAsync(request));
        }

        [HttpPost]
        [Route("save-mne-person")]
        public async Task<ActionResult> CreateMnePerson([FromForm] MnePersonDto guestDto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                if (User.IsInRole("TouristOrgOperator"))
                {
                    var g = _db.MnePersons.FirstOrDefault(a => a.Id == guestDto.Id);
                    if (g != null && (g.ExternalId ?? 0) != 0)
                    {
                        return Json(new BasicDto() { info = "", error = "Nemate prava da vršite izmjene na već prijavljenom gostu!" });
                    }
                }

                var newGuest = _mapper.Map<MnePersonDto, MnePerson>(guestDto);
                var property = _db.Properties.Include(a => a.LegalEntity).FirstOrDefault(a => a.Id == guestDto.PropertyId);
                //newGuest.LegalEntityId = property.LegalEntityId;

                var validation = _registerClient.Validate(newGuest, newGuest.CheckIn, newGuest.CheckOut);

                if (validation.ValidationErrors.Any())
                {
                    return Json(new BasicDto() { info = "", error = "", errors = validation.ValidationErrors });
                }

                await _registerClient.Initialize(property.LegalEntity);

                await _registerClient.Person(guestDto);

                return Json(new BasicDto() { info = "Uspješno sačuvan gost", error = "" });
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

        [HttpGet]
        [Route("post-office")]
        public IActionResult PostOffice()
        {
            return View();
        }

        [HttpGet]
        [Route("post-office-export")]
        public FileResult PostOfficeExport(string datum)
        {
            var date = DateTime.ParseExact(datum, "dd.MM.yyyy", CultureInfo.InvariantCulture);

            var partner = _db.Partners.Find(_appUser.PartnerId);

            var guests = _db.MnePersons.Include(a => a.Property).ThenInclude(a => a.LegalEntity)
                .Where(a => (a.UserCreatedDate ?? (DateTime?)a.CheckIn ?? DateTime.MinValue).Date == date)
                .Where(a => a.Property.LegalEntity.PartnerId == partner.Id)
                .Select(a => new { a.Property.LegalEntity.Name, a.Property.LegalEntity.TIN, Date = a.UserCreatedDate ?? a.CheckIn, Tax = (a.ResTaxAmount ?? 0m) })
                .ToList();

            var lines = guests.OrderBy(a => a.Date)
                .Select((a, b) =>
                    $"0|{partner.TIN}|{(b + 1).ToString("00000")}|0|{a.Name}|Uplata boravišne takse|TO Bar||{a.TIN}|{a.Tax.ToString("##0.00", new CultureInfo("en-US"))}|330|510-8093205-10|{a.Date.ToString("yyyyMMdd HH:mm:ss")}|0"
                    )
                .ToList();

            var txt = string.Join(Environment.NewLine, lines);

            return File(Encoding.UTF8.GetBytes(txt), "text/plain", $"{partner.Name}_PostOfficeExport_{date.ToString("yyyyMMdd")}.txt");
        }

        [HttpGet]
        [Route("post-office-report")]
        public FileResult PostOfficeReport(string datum)
        {
            var date = DateTime.ParseExact(datum, "dd.MM.yyyy", CultureInfo.InvariantCulture);

            var partner = _db.Partners.Find(_appUser.PartnerId);

            var toReport = $"{partner.Id}PostOffice";            
            var path = Path.Combine(_env.ContentRootPath, "Reports", toReport);

            var bytes = _reporting.RenderReport(
                path, 
                new List<Telerik.Reporting.Parameter>() { 
                    new Telerik.Reporting.Parameter(){ Name = "Date", Value = date },
                    new Telerik.Reporting.Parameter(){ Name = "PartnerId", Value = partner.Id },
                    new Telerik.Reporting.Parameter(){ Name = "CheckInPoint", Value = partner.Id },
                },
                "PDF");

            return File(bytes, "application/pdf");
        }

        [HttpGet]
        [Route("print-direct")]
        public IActionResult PrintDirect(int id)
        {
            var person = _db.MnePersons.Include(a => a.Property).Include(a => a.LegalEntity).FirstOrDefault(a => a.Id == id);

            return Json(new
            {
                from = $"{person.Property.LegalEntity.Name}\n{person.Property.LegalEntity.TIN}",
                to = "Opstina Bar",
                fromacc = "-",
                toacc = "510-8093205-10",
                desc = "Uplata boravisne takse",
                amount = ((person.ResTaxAmount).Value + (person.ResTaxFee ?? 0m)).ToString("#,###.00", new CultureInfo("de-DE"))
            });
        }

        [HttpGet]
        [Route("virman")]
        public IActionResult Virman(int id)
        {
            ViewBag.Id = id;
            return PartialView();
        }

        [HttpGet]
        [Route("virman-print")]
        public FileResult VirmanPrint(int id)
        {
            var result = _reporting.RenderReport("Virman", new List<Telerik.Reporting.Parameter>() { new Telerik.Reporting.Parameter("Id", id) }, "PDF");

            return File(result, "application/pdf");
        }
    }
}
