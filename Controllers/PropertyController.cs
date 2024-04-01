using AutoMapper;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
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
                if (_appUser.LegalEntity.Country == Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
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
            if (_appUser.LegalEntity.Country == Country.MNE)
            {
                municipalityList = codeLists.Where(x => x.Type == "opstina").ToList();
                typeList = codeLists.Where(x => x.Type == "vrstaobjekta").ToList();               
            }
            else if (_appUser.LegalEntity.Country == Country.SRB)
            {
                municipalityList = codeLists.Where(x => x.Type == "ResidenceMunicipality").ToList();
                typeList = codeLists.Where(x => x.Type == "Property_Type").ToList();
            }

            var municipalistySelectList = new SelectList(municipalityList, "ExternalId", "Name");
            var typeSelectList = new SelectList(typeList, "ExternalId", "Name");
            
            ViewBag.MunicipalityCodeList = municipalistySelectList;
            ViewBag.TypeCodeList = typeSelectList;

            ViewBag.LegalEntity = legalEntity;

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

            var data = _mapper.Map<List<PropertyEnrichedDto>>(properties);

            return Json(await data.ToDataSourceResultAsync(request));
        }

        [HttpPost]
        public async Task<ActionResult> Update(PropertyEnrichedDto input, [DataSourceRequest] DataSourceRequest request, [FromQuery]int? legalEntity)
        {
            try
            {
                var existingEntity = await _db.Properties.FindAsync(input.Id);

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }
                var dto = (PropertyDto)input;

                _mapper.Map(dto, existingEntity);

                existingEntity.PropertyName = existingEntity.Name;

                // validation

                await _db.SaveChangesAsync();

                return Json(new[] { input }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(PropertyEnrichedDto dto, [DataSourceRequest] DataSourceRequest request, [FromQuery] int? legalEntity)
        {
            try
            {
                var property = new Property();

                _mapper.Map(dto, property);

                property.PropertyName = property.Name;

                if (legalEntity.HasValue)
                {
                    _legalEntity = await _db.LegalEntities.FindAsync(legalEntity.Value);
                }

                property.LegalEntityId = _legalEntity.Id;                

                // validation

                _db.Add(property);
                await _db.SaveChangesAsync();

                return Json(new[] { dto }.ToDataSourceResult(request, ModelState));
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


        public async Task<IActionResult> ReadAdmin()
        {
            await _registerClient.Initialize(_legalEntity);
            var properties = await _registerClient.GetProperties();

            List<PropertyEnrichedDto> data = properties
                .Select(a => {
                    var mapped = _mapper.Map<Property, PropertyEnrichedDto>(a);
                    mapped.PropertyName = a.Name;
                    mapped.LegalEntity = a.LegalEntity.Name; 
                    return mapped;
                    })
                .ToList();

            return Json(data);            
        }
    }
}