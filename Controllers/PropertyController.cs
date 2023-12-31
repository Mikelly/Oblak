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
                if (_appUser.LegalEntity.Country == Country.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == Country.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
            }
        }

        [HttpGet]
        [Route("properties")]
        public async Task<IActionResult> Index()
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

            return View();
        }

        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            var properties = _db.Properties.Where(a => a.LegalEntityId == _legalEntityId);

            var data = _mapper.Map<List<PropertyEnrichedDto>>(properties);

            return Json(await data.ToDataSourceResultAsync(request));
        }

        [HttpPost("properties")]
        public async Task<ActionResult> Update(PropertyEnrichedDto input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var existingEntity = await _db.Properties.FindAsync(input.Id);

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(input, existingEntity);

                // validation

                await _db.SaveChangesAsync();

                return Json(new[] { _mapper.Map(existingEntity, input) }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, PropertyEnrichedDto model)
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

        public async Task<ActionResult> FetchPropertiesExternal()
        {
            try
            {
                var result = await _registerClient.Properties();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }
    }
}