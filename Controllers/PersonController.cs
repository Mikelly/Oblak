using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using Humanizer;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.DependencyResolver;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Helpers; 
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.Reporting;
using Oblak.Services.SRB;
using SQLitePCL;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using static SQLite.SQLite3;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Oblak.Controllers
{
    public class PersonController : Controller
    {
        private readonly Register _registerClient;
        private readonly ReportingService _reporting;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PersonController> _logger;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;
        private readonly LegalEntity _legalEntity;


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
                _legalEntity = _appUser.LegalEntity;
                if (_appUser.LegalEntity.Country == CountryEnum.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == CountryEnum.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
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

            if (_appUser.LegalEntity.Country == CountryEnum.SRB)
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

            if (_appUser.LegalEntity.Country == CountryEnum.MNE)
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
            ViewBag.TaxType = group?.VesselId == null ? "R" : "N";
            ViewBag.Nautical = group == null || group.VesselId == null ? "false" : "true";

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

            if (_appUser.LegalEntity.Country == CountryEnum.SRB)
            {
                return View("SrbPersons");
            }

            else if (_appUser.LegalEntity.Country == CountryEnum.MNE)
            {
                if (group != null)
                {
                    var restaxpay = _db.ResTaxPaymentTypes.FirstOrDefault(a => a.Id == group.ResTaxPaymentTypeId);
                    ViewBag.AlreadyPaid = restaxpay != null ? restaxpay.PaymentStatus == TaxPaymentStatus.AlreadyPaid : false;
                }
                else
                {                
                    ViewBag.AlreadyPaid = false; 
                }            

                return View("MnePersons");
            }

            return View("");
        }

        [HttpPost]
        [Route("mrz")]
        public async Task<ActionResult> Mrz(int? group, [FromBody] MrzDto mrz)
        {
            if (_appUser.LegalEntity.Country == CountryEnum.SRB)
            {

            }
            else if (_appUser.LegalEntity.Country == CountryEnum.MNE)
            {

            }

            return Ok();
        }

        void ParseMrz(MrzDto mrz, MnePersonEnrichedDto dto, List<CodeList> codeLists, int? vesselId, int? groupId, int? payType, int? exemptType)
        {
            var country = codeLists.Where(a => a.Type == "drzava" && a.ExternalId == mrz.DocIssuer.Replace("<", "")).FirstOrDefault();
            dto.PersonType = country.ExternalId == "MNE" ? "1" : "4";
            dto.LastName = mrz.HolderNamePrimary;
            dto.FirstName = mrz.HolderNameSecondary;
            dto.Nationality = mrz.HolderNationality;
            dto.BirthDate = mrz.HolderDateOfBirthDate();
            dto.Gender = mrz.HolderSex == "M" ? "M" : "Z";
            dto.PersonalNumber = mrz.HolderNumber;
            dto.DocumentCountry = country.ExternalId;
            dto.DocumentIssuer = mrz.DocAuthority;
            dto.DocumentNumber = (mrz.DocNumber ?? "").Replace("<", "");
            dto.DocumentValidTo = mrz.DocExpiryDate();
            dto.DocumentType = mrz.DocType == "IcaoTd1" || mrz.DocType == "IcaoTd2" ? "2" : "1";
            dto.BirthCountry = country.ExternalId;
            dto.PermanentResidenceCountry = country.ExternalId;
            dto.BirthPlace = country.Name;
            dto.PermanentResidenceAddress = country.Name;
            dto.PermanentResidencePlace = country.Name;
            dto.DocumentIssuer = country.ExternalId;
            var restax = ResTaxFoo(null, payType, exemptType, dto.BirthDate, dto.CheckIn, dto.CheckOut, vesselId != null, groupId != null);
            dto.ResTaxTypeId = restax.ResType;
            dto.ResTaxPaymentTypeId = restax.PayType;
            dto.ResTaxAmount = restax.Tax;
            dto.ResTaxFee = restax.Fee;
            dto.ResTaxExemptionTypeId = null;
            if (User.IsInRole("TouristOrg"))
            {
                if ((dto.PersonalNumber ?? "") == "")
                {
                    dto.PersonalNumber = dto.BirthDate.ToString("ddMMyyyy");
                }
            }
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

            var partner = _db.Partners.FirstOrDefault(a => a.Id == _appUser.PartnerId);
            
            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .ToListAsync();

            ViewBag.Group = group;
            ViewBag.Nautical = false;

            if (_appUser.LegalEntity.Country == CountryEnum.SRB)
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

            if (_appUser.LegalEntity.Country == CountryEnum.MNE)
            {
                Group g = null;
                MnePersonEnrichedDto dto = null;
                if (person == 0)
                {
                    dto = new MnePersonEnrichedDto();
                    if (group.HasValue && group != 0)
                    {
                        g = _db.Groups.Include(a => a.Property).FirstOrDefault(a => a.Id == group)!;
                        if (g != null)
                        {
                            dto.PropertyId = g.Property.Id;
                            dto.PropertyName = g.Property.Name;
                            dto.CheckIn = g.CheckIn.Value;
                            dto.CheckOut = g.CheckOut.Value;
                            dto.EntryPoint = g.EntryPoint;
                            dto.EntryPointDate = g.EntryPointDate;
                            dto.ResTaxPaymentTypeId = g.ResTaxPaymentTypeId;
                        }

                        if (mrz != null)
                        {
                            ParseMrz(mrz, dto, codeLists, g.VesselId, g.Id, g.ResTaxPaymentTypeId, null);
                        }


                    }
                    else
                    {
                        if (mrz != null)
                        {
                            dto.CheckIn = DateTime.Now;
                            dto.CheckOut = DateTime.Now.AddDays(1);

                            var country = codeLists.Where(a => a.Type == "drzava" && a.ExternalId == mrz.DocIssuer.Replace("<", "")).FirstOrDefault();
                            dto.PersonType = country.ExternalId == "MNE" ? "1" : "4";
                            dto.LastName = mrz.HolderNamePrimary;
                            dto.FirstName = mrz.HolderNameSecondary;
                            dto.Nationality = mrz.HolderNationality;
                            dto.BirthDate = mrz.HolderDateOfBirthDate();
                            dto.Gender = mrz.HolderSex == "M" ? "M" : "Z";
                            dto.PersonalNumber = mrz.HolderNumber;
                            dto.DocumentCountry = country.ExternalId;
                            dto.DocumentIssuer = mrz.DocAuthority;
                            dto.DocumentNumber = (mrz.DocNumber ?? "").Replace("<", "");
                            dto.DocumentValidTo = mrz.DocExpiryDate();
                            dto.DocumentType = mrz.DocType == "IcaoTd1" || mrz.DocType == "IcaoTd2" ? "2" : "1";
                            dto.BirthCountry = country.ExternalId;
                            dto.PermanentResidenceCountry = country.ExternalId;
                            dto.BirthPlace = country.Name;                            
                            dto.PermanentResidenceAddress = country.Name;
                            dto.PermanentResidencePlace = country.Name;
                            dto.DocumentIssuer = country.ExternalId;
                            var restax = ResTaxFoo(null, null, null, dto.BirthDate, dto.CheckIn, dto.CheckOut, false, false);
                            dto.ResTaxTypeId = restax.ResType;
                            dto.ResTaxPaymentTypeId = restax.PayType;
                            dto.ResTaxAmount = restax.Tax;
                            dto.ResTaxFee = restax.Fee;
                            if (User.IsInRole("TouristOrg"))
                            {
                                if ((dto.PersonalNumber ?? "") == "")
                                {
                                    dto.PersonalNumber = dto.BirthDate.ToString("ddMMyyyy");
                                }
                            }
                        }
                        else
                        {
                            dto.CheckIn = DateTime.Now;
                            dto.CheckOut = DateTime.Now.AddDays(1);
                            dto.BirthDate = DateTime.Now.AddYears(-18);
                            dto.DocumentValidTo = DateTime.Now.AddYears(10);
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

                    ViewBag.Nautical = g?.VesselId != null;

                    if (User.IsInRole("TouristOrg"))
                    {
                        if (dto.ResTaxPaymentTypeId == null)
                        {
                            dto.ResTaxPaymentTypeId = partner.DefaultPaymentId;
                        }
                    }

                    //ako se prijava vezuje za grupu onda se ne setuje DefaultPaymentId vec se cita iz grupe
                    if (!(group.HasValue && group != 0))
                    {
                        dto.ResTaxPaymentTypeId = partner.Id == 4 ? partner.DefaultPaymentId : dto.ResTaxPaymentTypeId;
                    } 
                }
                else
                {
                    var p = _db.MnePersons.Include(a => a.Property).Include(a => a.LegalEntity).Include(a => a.Group).Include(a => a.CheckInPoint).FirstOrDefault(a => a.Id == person);
                    g = p.Group;
                    ViewBag.Nautical = g?.VesselId != null;
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
                    ResTaxPaymentTypes = ((person.HasValue && person.Value > 0)
                                        ? _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.Status == "A")
                                        : _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.Status == "A" && a.Id != 6)
                                        ).ToDictionary(a => a.Id.ToString(), b => b.Description), 
                    ResTaxExemptionTypes = _db.ResTaxExemptionTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.Status == "A").ToDictionary(a => a.Id.ToString(), b => b.Description),
                    ResTaxTypes = _db.ResTaxTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.Status == "A").ToDictionary(a => a.Id.ToString(), b => b.Description)

                    //ResTaxStatuses = new Dictionary<string, string>() { { "Unpaid", "Nije plaćena" }, { "Cash", "Plaćena gotovinom" }, { "Card", "Plaćena karticom" }, { "BankAccount", "Plaćena virmanski" } },
                    //ResTaxTypes = new Dictionary<string, string>() { { "Unpaid", "Nije plaćena" }, { "Cash", "Plaćena gotovinom" }, { "Card", "Plaćena karticom" }, { "BankAccount", "Plaćena virmanski" } },
                };

                if (partner.CheckRegistered)
                {
                    if (dto.PropertyId != 0)
                    {
                        var legalEntity = _db.Properties.FirstOrDefault(a => a.Id == dto.PropertyId).LegalEntityId;
                        var balance = _db.TaxPaymentBalances.Where(a => a.TaxType == TaxType.ResidenceTax && a.LegalEntityId == legalEntity).FirstOrDefault();
                        var notRegistered = _db.Properties.Where(a => a.LegalEntityId == legalEntity).Any(a => a.RegDate == null || a.RegDate < DateTime.Now.Date);
                        if (notRegistered)
                        {
                            model.ResTaxPaymentTypes = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && (a.PaymentStatus != TaxPaymentStatus.Unpaid && a.PaymentStatus != TaxPaymentStatus.PaidInAdvance) && a.Status == "A").ToDictionary(a => a.Id.ToString(), b => b.Description);
                        }
                        else if (notRegistered == false && balance?.Balance <= 0)
                        {
                            model.ResTaxPaymentTypes = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.PaymentStatus != TaxPaymentStatus.PaidInAdvance && a.Status == "A").ToDictionary(a => a.Id.ToString(), b => b.Description);
                        }
                    }
                }

                var restaxpay = _db.ResTaxPaymentTypes.FirstOrDefault(a => a.Id == dto.ResTaxPaymentTypeId);
                ViewBag.AlreadyPaid = restaxpay != null ? restaxpay.PaymentStatus == TaxPaymentStatus.AlreadyPaid : false;
                ViewBag.AlreadyPaidIds = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.PaymentStatus == TaxPaymentStatus.AlreadyPaid && a.Status == "A").Select(a => a.Id).ToArray();

                ViewBag.Dto = dto;

                ViewBag.Disabled = false;
                ViewBag.TO = false;

                if (User.IsInRole("TouristOrg"))
                {
                    ViewBag.TO = true;
                    if (User.IsInRole("TouristOrgOperator"))
                    {
                        if (dto.ExternalId != null || dto.Status == "Closed") ViewBag.Disabled = true;
                    }
                }

                ViewBag.PartnerId = partner.Id;

                return PartialView("MnePerson", model);
            }

            return PartialView();
        }


        public ActionResult CopyLast()
        {
            var last = _db.MnePersons.Include(a => a.Property).Where(a => a.UserCreated == _appUser.UserName).OrderByDescending(a => a.UserCreatedDate).FirstOrDefault();

            if (last != null)
            {
                return Json(new { info = "OK", error = "", propertyId = last.PropertyId, propertyName = last.Property.Name, checkIn = last.CheckIn, checkOut = last.CheckOut ?? DateTime.Now.Date.AddDays(1), entryPoint = last.EntryPoint, entryPointDate = last.EntryPointDate });
            }
            else
            {
                return Json(new { info = "", error = "Ne postoji zadnja prijava" });
            }        
        }

        public ActionResult PrevInfo(string document, string country)
        {
            var last = _db.MnePersons.Include(a => a.Property).ThenInclude(a => a.LegalEntity)
                .Where(a => a.DocumentCountry == country && a.DocumentNumber == document)
                .OrderByDescending(a => a.UserCreatedDate)
                .FirstOrDefault();

            if (last != null)
            {
                ViewBag.Last = last;
                var ep = last.EntryPoint ?? "";
                ViewBag.EntryPointName = _db.CodeLists.Where(a => a.Country == "MNE" && a.Type == "prelaz" && a.ExternalId == ep).Select(a => a.Name).FirstOrDefault() ?? "";
                ViewBag.PartnerId = _appUser.PartnerId;
                return PartialView();
                //return Json(new { info = "OK", error = "", propertyId = last.PropertyId, propertyName = last.Property.Name, checkIn = last.CheckIn, checkOut = last.CheckOut ?? DateTime.Now.Date.AddDays(1) });
            }
            else
            {
                return Json(new { info = "", error = $"Gost sa brojem dokumenta {document} iz države ${country} nije prijavljen!" });
            }
        }
  
        public ActionResult AllPrevStays(string document, string country)
        {
            ViewBag.ShowSearch = string.IsNullOrEmpty(document) || string.IsNullOrEmpty(country);
            ViewBag.Document = document;
            ViewBag.Country = country;

            if (!ViewBag.ShowSearch)
            {
                var person = _db.MnePersons
                                .Where(a => a.LegalEntityId == _legalEntityId)
                                .Where(p => p.DocumentNumber == document && p.DocumentCountry == country) 
                                .FirstOrDefault();

                if (person != null)
                {
                    ViewBag.FirstName = person.FirstName;
                    ViewBag.LastName = person.LastName;
                    ViewBag.PersonId = person.Id;
                }
            }

            return PartialView();
        }

        public async Task<ActionResult> GetPrevStays([DataSourceRequest] DataSourceRequest request, string document, string country)
        {
            if (string.IsNullOrEmpty(document) || string.IsNullOrEmpty(country))
                return Json(Enumerable.Empty<MnePerson>());

            var stays = _db.MnePersons
                    .Include(a => a.Property).ThenInclude(a => a.LegalEntity)
                    .Where(a => a.LegalEntityId == _legalEntityId)
                    .Where(a => a.DocumentCountry == country && a.DocumentNumber == document)
                    .Skip(request.Skip)
                    .Take(request.PageSize)
                    .OrderByDescending(a => a.UserCreatedDate)
                    .Select(a => new PrevStayDto
                    {
                        PersonId = a.Id,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        DocumentNumber = a.DocumentNumber,
                        PropertyName = a.Property.Name,
                        PropertyAddress = a.Property.Address,
                        LegalEntityName = a.Property.LegalEntity.Name,
                        EntryPointDate = a.EntryPointDate,
                        CheckIn = a.CheckIn,
                        CheckOut = a.CheckOut,
                    })
                    .ToList();

            return Json(await stays.ToDataSourceResultAsync(request));
        }

        public async Task<ActionResult> SearchGuestAsync([DataSourceRequest] DataSourceRequest request, string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 4)
                return Json(Enumerable.Empty<object>());

            var names = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string firstName = names[0];
            string lastName = names.Length > 1 ? names[1] : "";
             
            var countryCode = _appUser.LegalEntity.Country.ToString();
             
            var documentTypeDictionary = await _db.CodeLists
                .Where(x => x.Country == countryCode && x.Type == "isprava")
                .ToDictionaryAsync(x => x.ExternalId, x => x.Name);
              
            var guests = _db.MnePersons
                .Where(a => a.LegalEntityId == _legalEntityId)
                .Where(p => p.FirstName.Contains(firstName) && p.LastName.Contains(lastName))
                .Take(100)
                .AsEnumerable()
                .DistinctBy(p => new
                {
                    FirstName = p.FirstName?.ToUpperInvariant(),
                    LastName = p.LastName?.ToUpperInvariant(),
                    p.BirthDate,
                    p.DocumentCountry,
                    p.DocumentType,
                    p.DocumentNumber
                })
                .Select(p => new
                {
                    PersonId = p.Id,
                    p.FirstName,
                    p.LastName,
                    BirthDate = p.BirthDate.ToString("dd.MM.yyyy"),
                    p.DocumentCountry,
                    DocumentType = documentTypeDictionary.GetValueOrNull(p.DocumentType),
                    p.DocumentNumber,
                    Display = $"{p.FirstName} {p.LastName} | {p.DocumentCountry} | {documentTypeDictionary.GetValueOrNull(p.DocumentType)} | {p.DocumentNumber}"
                })
                .ToList();
              
            return Json(guests);
        } 

        public ActionResult ResTax(int? resType, int? payType, int? exemptType, string birthDate, string checkIn, string checkOut, int group)
        {
            /*var p = _db.Properties.Include(a => a.LegalEntity).FirstOrDefault(a => a.Id == property);
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
            */

            DateTime? bd = null;
			DateTime? ci = null;
			DateTime? co = null;

			if (birthDate != null)
			{
				bd = DateTime.ParseExact(birthDate, "dd.MM.yyyy", null);				
			}
			if (checkIn != null && checkOut != null)
			{
				ci = DateTime.ParseExact(checkIn, "dd.MM.yyyy", null);
				co = DateTime.ParseExact(checkOut, "dd.MM.yyyy", null);
			}

			var result = ResTaxFoo(resType, payType, exemptType, bd, ci, co, false, group != 0);

            return Json(new { tax = result.Tax, fee = result.Fee, resType = result.ResType, payType = result.PayType });
        }


        private class RestTaxResult
        { 
            public decimal Tax { get; set; }
			public decimal Fee { get; set; }
			public int ResType { get; set; }
			public int PayType { get; set; }
		}


		private RestTaxResult ResTaxFoo(int? resType, int? payType, int? exemptType, DateTime? birthDate, DateTime? checkIn, DateTime? checkOut, bool nautical, bool group)
		{
            if (new int[] { 1, 2, 3, 27, 28, 29 }.ToList().Contains(resType ?? 0) == false && resType != null || exemptType != null)
            {
                return new RestTaxResult { Tax = 0, Fee = 0, ResType = resType.Value, PayType = 1 };
            }

            //var p = _db.Properties.Include(a => a.LegalEntity).FirstOrDefault(a => a.Id == property);
            //var pid = p.LegalEntity.PartnerId;
            var pid = _appUser.LegalEntity.PartnerId;
			var rt = _db.ResTaxTypes.FirstOrDefault(a => a.Id == resType);
			var pt = _db.ResTaxPaymentTypes.FirstOrDefault(a => a.Id == payType);

			DateTime? bd = birthDate;
			DateTime? ci = checkIn;
			DateTime? co = checkOut;

			if (birthDate != null)
			{	
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
				days = (int)(co.Value.Date - ci.Value.Date).TotalDays;
				if (days < 0) days = 0;
			}

			if (rt != null)
			{
				tax = rt.Amount * days;
			}

            if (pt == null)
            {
                pt = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == pid).Where(a => a.PaymentStatus == TaxPaymentStatus.Cash).FirstOrDefault();
			}

            if (pt != null && group == false)
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

			return new RestTaxResult { Tax = tax, Fee = fee, ResType = rt?.Id ?? 0, PayType = pt?.Id ?? 0 };
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

        public async Task<ActionResult> ReadMnePersons([DataSourceRequest] DataSourceRequest request, int groupId, string period = "D", string dtmod = null, string dtmdo = null)
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

            var query = _db.MnePersons.Include(a => a.Property).ThenInclude(a => a.LegalEntity).Include(a => a.CheckInPoint).Include(a => a.Computer)
                .Where(a => a.GroupId == groupId && groupId != 0 || groupId == 0 && a.GroupId == null)
                .Where(a => a.LegalEntityId == _legalEntityId);

            if (User.IsInRole("TouristOrgOperator"))
            {
                var user = User.Identity.Name;
                query = query.Where(a => a.UserCreated == user || a.UserCreated == "unknown");
            }

            if (groupId != 0)
            {
                query = query.Where(a => a.GroupId == groupId);
                if (User.IsInRole("TouristOrgOperator"))
                {
                    query = query.Where(a => a.CheckInPointId == _appUser.CheckInPointId);
                }
            }
            else
            {
                DateTime now = DateTime.Now;
                DateTime Od = now;
                DateTime Do = now;

                CalcPeriod(now, period, dtmod, dtmdo, out Od, out Do);

                query = query.Where(a => a.UserCreatedDate >= Od && a.UserCreatedDate <= Do);
            }

            var to = User.IsInRole("TouristOrgOperator");

            if (to)
            {
                var user = User.Identity.Name;
                query = query.Where(a => a.UserCreated == user || a.UserCreated == "unknown");
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
                    ResTaxAmount = a.ResTaxAmount,
                    ResTaxFee = a.ResTaxFee,
                    DocumentType = documentTypeDictionary.GetValueOrNull(a.DocumentType),
                    DocumentNumber = a.DocumentNumber,
                    DocumentValidTo = a.DocumentValidTo,
                    DocumentIssuer = a.DocumentIssuer,
                    DocumentCountry = a.DocumentCountry,
                    EntryPoint = a.EntryPoint,
                    EntryPointDate = a.EntryPointDate,
                    VisaType = a.VisaType,
                    VisaNumber = a.VisaNumber,
                    VisaIssuePlace = a.VisaIssuePlace,
                    VisaValidFrom = a.VisaValidFrom,
                    VisaValidTo = a.VisaValidTo,
                    Registered = a.ExternalId != null,
                    Deleted = a.IsDeleted,
                    UserCreated = a.UserCreated,
                    UserCreatedDate = a.UserCreatedDate,
                    CheckInPointName = a.CheckInPoint.Name,
                    LegalEntityName = a.Property.LegalEntity.Name,
                    ComputerCreated = a.Computer.PCName
                });

            return Json(await data.ToDataSourceResultAsync(request));
        }

        [HttpPost]
        [Route("save-mne-person")]
        public async Task<ActionResult> CreateMnePerson([FromForm] MnePersonDto guestDto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var partner = _db.Partners.FirstOrDefault(a => a.Id == _appUser.PartnerId);
                var guest = _db.MnePersons.FirstOrDefault(a => a.Id == guestDto.Id);
                  
                if (guest != null && guest.ResTaxStatus == ResTaxStatus.Closed)
                {
                    if (User.IsInRole("TouristOrgOperator"))
                    {
                        return Json(new
                        {
                            info = "",
                            error = "Nemate prava da vršite izmjene na već prijavljenom gostu!",
                            id = guest.Id,
                            guest.ResTaxTypeId,
                            guest.ResTaxAmount,
                            guest.ResTaxFee,
                            guest.UserCreated
                        });

                    }
                }

                var newGuest = _mapper.Map<MnePersonDto, MnePerson>(guestDto);

                #region Linkovanje sa registrovanim racunarom za create new person
                if (guestDto.Id == 0)
                {
                    if(Request.Cookies.TryGetValue("device_id", out var deviceId) && Guid.TryParse(deviceId, out var guid))
                    {
                        var computer = _db.Computers.FirstOrDefault(x => x.Id == guid);
                        if (computer != null)
                        { 
                            guestDto.ComputerCreatedId = computer.Id;
                        }
                        else
                            guestDto.ComputerCreatedId = null; //unknow
                    }
                    else
                        guestDto.ComputerCreatedId = null; //unregistred
                }
                else
                {
                    guestDto.ComputerCreatedId = guest?.ComputerCreatedId; //zadrzavamo
                } 
                #endregion

                _db.Entry(newGuest).Reference(a => a.LegalEntity).Load();
                var property = _db.Properties.Include(a => a.LegalEntity).FirstOrDefault(a => a.Id == guestDto.PropertyId);

                if (property == null)
                {
					return Json(new BasicDto() { info = "", error = "Nijeste odabrali smještajni objekat!", id = 0 });
				}

				//newGuest.LegalEntityId = property.LegalEntityId;

				if (User.IsInRole("TouristOrgOperator") || User.IsInRole("TouristOrgAdmin") || User.IsInRole("TouristOrgControllor"))
				{

                }

				var validation = _registerClient.Validate(newGuest, newGuest.CheckIn, newGuest.CheckOut);

                if (validation.ValidationErrors.Any())
                {
                    return Json(new BasicDto() { info = "", error = "", errors = validation.ValidationErrors });
                }

                if (User.IsInRole("TouristOrgOperator") || User.IsInRole("TouristOrgAdmin") || User.IsInRole("TouristOrgControllor"))
                {
					var pass = property.LegalEntity.PassThroughId;
					if (pass.HasValue)
					{
                        var passLegalEntity = _db.LegalEntities.FirstOrDefault(a => a.Id == pass.Value);
						await _registerClient.Initialize(passLegalEntity);
					}
				}
                else
                {
                    await _registerClient.Initialize(property.LegalEntity);
                }

				var hist = new ResTaxHistory();
                if (guest != null)
                {
                    hist.PersonId = guest.Id;
                    hist.PrevCheckIn = guest.CheckIn;
                    hist.PrevCheckOut = guest.CheckOut;
                    hist.PrevResTaxAmount = guest.ResTaxAmount;
                    hist.PrevResTaxPaymentTypeId = guest.ResTaxPaymentTypeId;
                    hist.PrevResTaxExemptionTypeId = guest.ResTaxExemptionTypeId;
                    hist.PrevResTaxTypeId = guest.ResTaxTypeId;
                }

				var person = await _registerClient.Person(guestDto);

                if ((User.IsInRole("TouristOrgControllor") || User.IsInRole("TouristOrgAdmin")) && guest != null && guest.ResTaxStatus == ResTaxStatus.Closed)
                {
                    _db.ResTaxHistory.Add(hist);
                    _db.SaveChanges();
                }

                //if (person.GroupId != null)
                //{
                //    var g = _db.Groups.FirstOrDefault(a => a.Id == person.GroupId);

                //    if (g.VesselId == null)
                //    {
                //        g.ResTaxAmount = _db.MnePersons.Where(a => a.GroupId == g.Id).Select(a => a.ResTaxAmount).Sum();
                //        if (g.ResTaxPaymentTypeId != null)
                //        {
                //            g.ResTaxFee = (_registerClient as MneClient).CalcResTaxFee(g.ResTaxAmount ?? 0, _appUser.PartnerId ?? 1, g.ResTaxPaymentTypeId ?? 0);
                //            g.ResTaxCalculated = true;
                //            g.ResTaxPaid = false;
                //        }
                //    }
                //}

                _db.Entry(person).Reference(a => a.LegalEntity).Load();
                _db.Entry(person).Reference(a => a.Property).Load();

                if (partner.CheckRegistered)
                {
					var balance = _db.GetBalance("ResidenceTax", person.Property.LegalEntityId, 0);
                    var balance_record = _db.TaxPaymentBalances.Where(a => a.LegalEntityId == person.LegalEntityId && a.TaxType == TaxType.ResidenceTax).FirstOrDefault();
                    if(balance_record == null)
                    {
                        balance_record = new TaxPaymentBalance();
                        balance_record.TaxType = TaxType.ResidenceTax;
                        balance_record.LegalEntityId = person.Property.LegalEntityId;
                        _db.TaxPaymentBalances.Add(balance_record);
                    }
                    balance_record.Balance = balance;
                    _db.SaveChanges();
				}

                var mnep = person as MnePerson;

                return Json(new { info = "Uspješno sačuvan gost", error = "", id = person.Id, mnep.ResTaxTypeId, mnep.ResTaxAmount, mnep.ResTaxFee, mnep.UserCreated });
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpGet]
        [Route("delete-mne-person")]
        public JsonResult DeleteMnePerson(int guestId)
        {
            try
            {
                var guest = _db.MnePersons.Include(a => a.Property)
                    .Include(a => a.Group)
                    .Include(a => a.Property)
                    .ThenInclude(a => a.LegalEntity)
                    .FirstOrDefault(a => a.Id == guestId);

                var partner = _db.Partners.FirstOrDefault(a => a.Id == _appUser.PartnerId);
                int? le = null;

                if (guest != null)
                {
                    if (User.IsInRole("TouristOrgOperator"))
                    {
                        return Json(new { error = "Nemate prava brisati gosta", info = "" });
                    }

                    le = guest.Property.LegalEntityId;
                    var groupId = guest.GroupId;

                    var hist = new ResTaxHistory();
					hist.PrevCheckIn = guest.CheckIn;
					hist.PrevCheckOut = guest.CheckOut;
					hist.PrevResTaxAmount = guest.ResTaxAmount;
					hist.PrevResTaxPaymentTypeId = guest.ResTaxPaymentTypeId;
					hist.PrevResTaxExemptionTypeId = guest.ResTaxExemptionTypeId;
                    hist.PrevResTaxTypeId = guest.ResTaxTypeId;
                       
                    guest.UserModifiedDate = DateTime.Now; 
                    _db.SaveChanges();

                    _db.MnePersons.Remove(guest);
                    _db.SaveChanges();

                    if (User.IsInRole("TouristOrg"))
                    {
                        _db.ResTaxHistory.Add(hist);
                        _db.SaveChanges();
                    }

                    // azuriranje taksi grupe, ako je gost bio clan neke grupe
                    if (groupId.HasValue)
                    {
                        var group = _db.Groups
                            .Include(g => g.Persons)
                            .FirstOrDefault(g => g.Id == groupId.Value);

                        if (group != null)
                        { 
                            _db.Entry(group).Collection(g => g.Persons).Load(); 
                            _db.Entry(group).Reference(g => g.ResTaxPaymentType).Load();

                            (_registerClient as MneClient)!.CalcGroupResTax(group, group.ResTaxPaymentType?.PaymentStatus ?? TaxPaymentStatus.None);
                            _db.SaveChanges();
                        }
                    }


                    if (partner.CheckRegistered)
                    {
                        var balance = _db.TaxPaymentBalances.Where(a => a.LegalEntityId == le && a.TaxType == TaxType.ResidenceTax).FirstOrDefault();
                        if (balance == null)
                        {
                            balance = new TaxPaymentBalance();
                            balance.LegalEntityId = le;  
                            balance.TaxType = TaxType.ResidenceTax;
                            _db.Add(balance);
                            _db.SaveChanges();
                        }

                        var calc = _db.GetBalance("ResidenceTax", le, 0);
                        balance.Balance = calc;
                        _db.SaveChanges();
                    }

                    return Json(new { info = "Gost je uspješno obrisan", error = "" });
                }
                else
                {
                    return Json(new { error = "Gost nije pronađen", info = "" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja gosta" });
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
                _db.Entry(newGuest).Reference(a => a.LegalEntity).Load();

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
        public IActionResult PostOffice(string tax = "R")
        {
            ViewBag.Tax = tax;
			(TaxType taxType, string taxDesc) = DecodeTaxType(tax);
            ViewBag.TaxDesc = taxDesc;
			return View("PostOffice");
        }

        [HttpGet]
        [Route("post-office-res")]
        public IActionResult PostOfficeRes()
        {
            return PostOffice("R");
        }

        [HttpGet]
        [Route("post-office-exc")]
        public IActionResult PostOfficeExc()
        {
            return PostOffice("E");
        }

        [HttpGet]
        [Route("post-office-nau")]
        public IActionResult PostOfficeNau()
        {
            return PostOffice("N");
        }

        //public class PostOfficeData
        //{
        //    public string Name { get; set; }
        //    public string TIN { get; set; }
        //    public DateTime Date { get; set; }
        //    public decimal Tax { get; set; }
        //}

        public class PostOfficeData
        {
            public string OrderNo { get; set; }
            public string Account { get; set; }
            public string Description { get; set; }
            public string RecipientName { get; set; }
            public string PayeeName { get; set; }
            public string PayeeAddress { get; set; }
            public string TIN { get; set; }
			public string Guest { get; set; }
			public decimal Tax { get; set; }
            public decimal Fee { get; set; }
            public string UserCreated { get; set; }
            public DateTime UserCreatedDate { get; set; }
            public string CheckInPointName { get; set; }
        }

        [HttpGet]
        [Route("post-office-export")]
        public FileResult PostOfficeExport(string datum, int? chekinpointid, string tax = "R")
        {
            try
            {
                var date = DateTime.ParseExact(datum, "dd.MM.yyyy", CultureInfo.InvariantCulture);

                (TaxType taxType, string taxDesc) = DecodeTaxType(tax);
                var taxName = Enum.GetName(typeof(TaxType), taxType);

                var partner = _db.Partners.Find(_appUser.PartnerId);

                var checkInPoint = _appUser.CheckInPointId;

                if (chekinpointid.HasValue) checkInPoint = chekinpointid;

                var sql = $"EXEC TouristOrgPostOffice '{date.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpper()}', {partner.Id}, {checkInPoint.Value}, '{taxName}', 0, 0, 0, 0";
                var data = _db.Database.SqlQuery<PostOfficeData>(FormattableStringFactory.Create(sql)).ToList();

                var s = _db.PartnerTaxSettings.FirstOrDefault(a => a.TaxType == taxType && a.PartnerId == _appUser.PartnerId);

                //var data = dataFromDb.Select(a => new PostOfficeData() { Name = a.PayeeName, Date = a.UserCreatedDate, Tax = a.Tax + a.Fee, TIN = a.TIN }).ToList();

                var lines = data.OrderBy(a => a.UserCreatedDate)
                    .Select((a, b) =>
                        $"0|{partner.TIN}|{(b + 1).ToString("00000")}|0|{a.PayeeName}|{s.PaymentDescription}|{s.PaymentName}|{s.Model}|{a.TIN}|{a.Tax.ToString("##0.00", new CultureInfo("en-US"))}|{s.Code}|{s.PaymentAccount}|{a.UserCreatedDate.ToString("yyyyMMdd HH:mm:ss")}|0"
                        )
                    .ToList();

                var txt = string.Join(Environment.NewLine, lines);

                return File(Encoding.UTF8.GetBytes(txt), "text/plain", $"{partner.Name}_PostOfficeExport_{s.PaymentDescription}_{date.ToString("yyyyMMdd")}.txt");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        (TaxType, string) DecodeTaxType(string tax)
        {
            var taxType = tax switch
            {
                "R" => TaxType.ResidenceTax,
                "N" => TaxType.NauticalTax,
                "E" => TaxType.ExcursionTax,
                _ => TaxType.ResidenceTax
            };

            var taxDesc = tax switch
            {
                "R" => "Boravišna taksa",
                "N" => "Nautička taksa",
                "E" => "Izletnička taksa",
                _ => "Boravišna taksa"
            };

            return (taxType, taxDesc);
        }

        [HttpGet]
        [Route("post-office-report")]
        public FileResult PostOfficeReport(string datum, string tax = "R")
        {
            (TaxType taxType, string taxDesc) = DecodeTaxType(tax);
            var taxName = Enum.GetName(typeof(TaxType), taxType);

            var date = DateTime.ParseExact(datum, "dd.MM.yyyy", CultureInfo.InvariantCulture);

            var partner = _db.Partners.Find(_appUser.PartnerId);
            var chekinpoint = _appUser.CheckInPointId;

            var cp = _db.CheckInPoints.Find(_appUser.CheckInPointId);

            var toReport = $"{partner.Id}PostOffice";            
            var path = Path.Combine(_env.ContentRootPath, "Reports", toReport);

            var bytes = _reporting.RenderReport(
                path, 
                new List<Telerik.Reporting.Parameter>() { 
                    new Telerik.Reporting.Parameter(){ Name = "Date", Value = date },
                    new Telerik.Reporting.Parameter(){ Name = "PartnerId", Value = partner.Id },
                    new Telerik.Reporting.Parameter(){ Name = "CheckInPoint", Value = chekinpoint },
                    new Telerik.Reporting.Parameter(){ Name = "TaxType", Value = taxName },
                    new Telerik.Reporting.Parameter(){ Name = "id", Value = 0 },
                    new Telerik.Reporting.Parameter(){ Name = "g", Value = 0 },
                    new Telerik.Reporting.Parameter(){ Name = "inv", Value = 0 },
                    new Telerik.Reporting.Parameter(){ Name = "pay", Value = 0 },
                    new Telerik.Reporting.Parameter(){ Name = "CheckInPointName", Value = cp.Name },
                    new Telerik.Reporting.Parameter(){ Name = "TaxTypeName", Value = taxDesc },
                },
                "PDF");

            return File(bytes, "application/pdf");
        }

        class VirmanData
        { 
            public string from { get; set; } = "";
            public string to { get; set; } = "";
            public string fromacc { get; set; } = "";
            public string toacc { get; set; } = "";
            public string desc { get; set; } = "";
            public string amount { get; set; } = "";
            public string tax { get; set; } = "";
            public string fee { get; set; } = "";
            public string id { get; set; } = "";
            public string refdeb { get; set; } = "";
            public string refcre { get; set; } = "";
            public string addr { get; set; } = "";
            public string model { get; set; } = "18";
            public string code { get; set; } = "030";
        }

        private string IntOrNull(int? v)
        {
            if (v.HasValue) return v.Value.ToString();
            else return "NULL";
        }

        [HttpGet]
        [Route("print-direct")]
        public IActionResult PrintDirect(int? id, int? g, int? inv, int? pay, string tax = "R")
        {
            (TaxType taxType, string taxDesc) = DecodeTaxType(tax);
            taxDesc = taxDesc.ToAscii();
            var taxName = Enum.GetName(typeof(TaxType), taxType);

            var partner = _db.Partners.FirstOrDefault(x => x.Id == _appUser.PartnerId);
            var settings = _db.PartnerTaxSettings.Where(a => a.PartnerId == _appUser.PartnerId && a.TaxType == taxType).FirstOrDefault();
            if (settings == null)
            { 
                return BadRequest($"Nema konfiguracije za TaxType '{taxType}' i partner '{partner.Id}'.");
            }

            var sql = $"EXEC TouristOrgPostOffice '{DateTime.Now.Date.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpper()}', {partner.Id}, {0}, '{taxName}', {id ?? 0}, {g ?? 0}, {inv ?? 0}, {pay ?? 0}";
			var po = _db.Database.SqlQuery<PostOfficeData>(FormattableStringFactory.Create(sql)).ToList().FirstOrDefault();

            if (id.HasValue)
            {
                var person = _db.MnePersons.Include(a => a.Property).ThenInclude(a => a.LegalEntity).Include(a => a.LegalEntity).Include(a => a.CheckInPoint).FirstOrDefault(a => a.Id == id);
                person.ResTaxStatus = ResTaxStatus.Closed;
                _db.SaveChanges();
            }

            var address = po.PayeeAddress
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Trim();

            var ci = new CultureInfo("de-DE");
            if(partner.Culture != null) ci = new CultureInfo(partner.Culture);

            var data = new VirmanData();
            data.from = $"{po.PayeeName}\n{address}";
            data.to = $"{po.RecipientName}{(po.CheckInPointName != null ? Environment.NewLine + po.CheckInPointName : "")}";
            data.toacc = po.Account;
            data.refcre = po.TIN;
            if (partner.SplitTaxAndFee)
            {
                data.tax = (po.Tax).ToString("#,##0.00", ci);
                data.fee = (po.Fee).ToString("#,##0.00", ci);
                data.amount = (po.Tax).ToString("#,##0.00", ci);
            }
            else
            {
                data.tax = "";
                data.fee = "";
                data.amount = (po.Tax + po.Fee).ToString("#,##0.00", ci);
            }
            data.addr = settings.PaymentAddress;
            data.desc = $"{po.Description}";
            if (id.HasValue)
            {
                data.desc += $"\n{po.Guest}";
                data.id = $"U{id}";
            }
            if (g.HasValue && g > 0)
            {
                data.id = $"G{g}";
            }
            if (inv.HasValue && inv > 0)
            {
                data.id = $"I{inv}";
            }
            if (pay.HasValue && pay > 0)
            {
                data.id = $"P{pay}";
            }

            if(taxType == TaxType.NauticalTax)
            {
                data.from = $"{po.PayeeName}\n{address}";
                data.to = _appUser.PartnerId == 4 ? "Turistička organizacija Opstine Budva" : "Turistička organizacija Opstine Bar";
                data.desc = $"Uplata prijave boravka\nVlasnik plovila:{po.PayeeName}\nZa period:{po.Description}";
            }

            return Json(data);
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

        [HttpGet]
        [Route("guest-list")]
        public async Task<ActionResult> GuestList()
        {
            await _registerClient.Initialize(_legalEntity);
            var properties = await _registerClient.GetProperties();

            if (properties.Count == 1)
            {
                ViewBag.PropertyId = properties[0].Id.ToString();
                ViewBag.PropertyName = properties[0].Name;
            }
            else
            {
                ViewBag.PropertyId = "0";
                ViewBag.PropertyName = "";
            }
            ViewBag.PartnerId = _appUser.PartnerId?.ToString() ?? "";
            ViewBag.Properties = properties.Select(a => { var b = _mapper.Map<Property, PropertyDto>(a); b.LegalEntityName = a.LegalEntity.Name; return b; }).ToList();

            return View();
        }

        [HttpGet]
        [Route("guest-list-grid")]
        public async Task<ActionResult> GuestListGrid(int objekat, string datumod, string datumdo)
        {
            ViewBag.Objekat = objekat;
            ViewBag.DatumOd = datumod;
            ViewBag.DatumDo = datumdo;

            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> GuestListRead([DataSourceRequest] DataSourceRequest request, int objekat, string datumod, string datumdo)
        {
            var obj = _db.Properties.FirstOrDefault(a => a.Id == objekat);

            var OD = DateTime.ParseExact(datumod, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var DO = DateTime.ParseExact(datumdo, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

            if (_registerClient is MneClient)
            {
                var guestlist = await _db.Set<MneGuestListDto>()
                                        .FromSqlInterpolated(
                                                $"EXEC PersonListMne @property = {objekat}, @od = {OD}, @do = {DO}"
                                            )
                                            .ToListAsync(); 

                return Json(await guestlist.ToDataSourceResultAsync(request));
            }
            else if (_registerClient is SrbClient)
            {  
                var guestlist = await _db.Set<MneGuestListDto>()
                                        .FromSqlInterpolated(
                                                $"EXEC SrbPersonList @property = {objekat}, @od = {OD}, @do = {DO}"
                                            )
                                            .ToListAsync();

                return Json(await guestlist.ToDataSourceResultAsync(request));
            }
            else return null;
        }

        [HttpGet]
        [Route("guest-list-print")]
        public async Task<FileResult> GuestListPrint(int objekat, string datumod, string datumdo, int? partnerId)
        {

            var stream = await _registerClient.GuestListPdf(objekat, datumod, datumdo, partnerId);

            stream.Seek(0, SeekOrigin.Begin);
            var fsr = new FileStreamResult(stream, "application/pdf");
            fsr.FileDownloadName = $"KnjigaGostiju.pdf";
            return fsr;
        }
    }
}
