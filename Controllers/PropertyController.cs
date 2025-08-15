using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Helpers;
using Oblak.Models;
using Oblak.Models.Api; 
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace Oblak.Controllers
{
    public class PropertyController : Controller
    {
        private readonly ILogger<PropertyController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;
        private readonly Register _registerClient;
        private LegalEntity _legalEntity;

        public PropertyController(
            ILogger<PropertyController> logger,
            ApplicationDbContext db,
            IMapper mapper,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;

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
        [Route("properties")]
        public async Task<IActionResult> Index(string how, int? legalEntity)
        {
            var codeLists = await _db.CodeLists
                .Where(a => a.Country == _appUser.LegalEntity.Country.ToString())
                .ToListAsync();

            var municipalityList = new List<CodeList>();
            var typeList = new List<CodeList>();
            SelectList opstine = new SelectList(new List<string>()); 

            if (_appUser.LegalEntity.Country == CountryEnum.MNE)
            {
                municipalityList = _db.Municipalities.Where(a => a.Country == _appUser.LegalEntity.Country).Select(a => new CodeList { ExternalId = a.Id.ToString(), Name = a.Name }).ToList();
                typeList = codeLists.Where(x => x.Type == "vrstaobjekta").ToList();

                opstine = new SelectList(_db.Municipalities.Where(a => a.Country == CountryEnum.MNE).ToList(), "Id", "Name"); 
            }
            else if (_appUser.LegalEntity.Country == CountryEnum.SRB)
            {
                municipalityList = codeLists.Where(x => x.Type == "ResidenceMunicipality").ToList();
                typeList = codeLists.Where(x => x.Type == "Property_Type").ToList();

                opstine = new SelectList(_db.Municipalities.Where(a => a.Country == CountryEnum.SRB).ToList(), "Id", "Name");
            }

            var municipalistySelectList = new SelectList(municipalityList, "ExternalId", "Name");
            var typeSelectList = new SelectList(typeList, "ExternalId", "Name");
            var statusSelectList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = "A", Text = "Aktivan" },
                new SelectListItem { Value = "N", Text = "Neaktivan" }
            }, "Value", "Text");

            ViewBag.MunicipalityCodeList = municipalistySelectList;
            ViewBag.TypeCodeList = typeSelectList;
            ViewBag.StatusList = statusSelectList;

            ViewBag.Opstine = opstine;
            ViewBag.Places = new SelectList(_db.CodeLists.Where(a => a.Type == "mjesto").ToList(), "ExternalId", "Name");

            ViewBag.LegalEntity = legalEntity ?? _appUser.LegalEntity.Id;

           if (how == "P")
            {
                ViewBag.Partial = true;
                return PartialView();
            }
            else
            {
                ViewBag.Partial = false;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request, int? legalEntity)
        {
            if (legalEntity.HasValue)
            {
                _legalEntity = await _db.LegalEntities.FindAsync(legalEntity.Value);
            }

            await _registerClient.Initialize(_legalEntity);
            var properties = await _registerClient.GetProperties();

            var data = _mapper.Map<List<PropertyEnrichedDto>>(properties).OrderByDescending(x => x.Id); 

            return Json(await data.ToDataSourceResultAsync(request));
        }

        [HttpPost]
        public async Task<ActionResult> Update(PropertyEnrichedDto input, [DataSourceRequest] DataSourceRequest request, [FromQuery] int? legalEntity)
        {
            try
            {
                Property? existingEntity = _db.Properties.Where(a => a.Id == input.Id).FirstOrDefault();

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Property not found." });
                }
                  
                existingEntity.Name = input.Name;
                existingEntity.ExternalId = input.ExternalId ?? 0;
                existingEntity.MunicipalityId = input.MunicipalityId;
                existingEntity.Place = input.Place;
                existingEntity.RegNumber = input.RegNumber;
                existingEntity.RegDate = input.RegDate;
                existingEntity.Type = input.Type;
                existingEntity.Status = input.Status;
                existingEntity.Address = input.Address; 

                _db.Update(existingEntity);
                _db.Entry(existingEntity).State = EntityState.Modified;

                //_mapper.Map(input, existingEntity);

                _db.SaveChanges(); 

                return Json(new[] { input }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(PropertyEnrichedDto input, [DataSourceRequest] DataSourceRequest request, [FromQuery] int? legalEntity)
        {
            try
            {
                if (input.MunicipalityId == null)
                {
                    ModelState.AddModelError("MunicipalityId", "Opština je obavezna.");
                }
                if (!ModelState.IsValid)
                {
                    return Json(new DataSourceResult { Errors = ModelState });
                }

                var property = new Property();

                property.Name = input.Name;
                property.MunicipalityId = input.MunicipalityId;
                property.Place = input.Place;
                property.RegNumber = input.RegNumber;
                property.RegDate = input.RegDate;
                property.Type = input.Type;
                property.Status = input.Status;
                property.Address = input.Address;

                property.PropertyName = input.Name;

                if (legalEntity.HasValue)
                {
                    _legalEntity = await _db.LegalEntities.FindAsync(legalEntity.Value);
                }

                property.LegalEntityId = _legalEntity.Id; 

                _db.Properties.Add(property);
                _db.SaveChanges();

                return Json(new[] { input }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, PropertyEnrichedDto model, [FromQuery] int? legalEntity)
        {
            try
            {
                //return Json(new { error = "Nije dostupno." });

                var property = await _db.Properties.Include(x => x.Groups)
                    .Where(x => x.Id == model.Id)
                    .FirstOrDefaultAsync();

                if (property != null)
                {
                    _db.Groups.RemoveRange(property.Groups);
                    _db.Properties.Remove(property);
                    _db.SaveChanges();
                    return Json(new { info = "Objekat uspješno obrisan." });
                }
                else
                {
                    return Json(new { error = "Objekat nije pronađen." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja objekta." });
            }
        }

        public async Task<ActionResult> FetchPropertiesExternal(int? legalEntity)
        {
            try
            {
                if (legalEntity.HasValue)
                {
                    _legalEntity = await _db.LegalEntities.FindAsync(legalEntity.Value);
                }

                var result = await _registerClient.Properties(_legalEntity);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }


        public async Task<IActionResult> ReadAdmin(string text)
        {
            await _registerClient.Initialize(_legalEntity);
            var properties = await _registerClient.GetProperties();
            text = (text ?? "") .ToLower();

            List<PropertyEnrichedDto> data = properties
                .Where(a => a.Name.ToLower().Contains(text) || a.LegalEntity.Name.ToLower().Contains(text) || a.LegalEntity.TIN.Contains(text))
                .Select(a => {
                    var mapped = _mapper.Map<Property, PropertyEnrichedDto>(a);
                    mapped.PropertyName = a.Name;
                    mapped.LegalEntity = a.LegalEntity.Name;
                    mapped.TIN = a.LegalEntity.TIN;
                    return mapped;
                })
                .Take(50)
                .ToList();

            return Json(data);
        }

        public async Task<IActionResult> Opstine()
        {
            var data = _db.Municipalities.ToList();

            return Json(data);
        }
                
        public async Task<IActionResult> Mjesta(int opstina, [DataSourceRequest] DataSourceRequest request)
        {
            if (opstina == 0) 
                return Json(new List<object>()); 

            var m = _db.Municipalities.FirstOrDefault(a => a.Id == opstina); 
            var txt = Request.Query["filter[filters][0][value]"].ToString()?.Trim() ?? "";

            var nam = Request.Query["filter[filters][0][field]"].ToString() ?? "";

            var data = _db.CodeLists.Where(a => a.Type == "mjesto" && a.Param1 == m.ExternalId)
                                    .AddMappedLocation(m.ExternalId, "RAFAILOVIĆI", "BEČIĆI");

            if (nam == "Name")
            { 
                data = data.Where(a => a.Name.ToLower().Contains(txt.ToLower()) || string.IsNullOrEmpty(txt)); 
            }

            return Json(data.ToList());
      }


        [HttpGet]
        [Route("openExportInvoices")]
        public IActionResult OpenExportInvoices(int legalEntityId, int propertyId)
        {
            var model = new ExportInvoicesDto
            {
                LegalEntityId = legalEntityId,
                PropertyId = propertyId
            };
            return PartialView("_ExportInvoices", model); 
        }

    }
}