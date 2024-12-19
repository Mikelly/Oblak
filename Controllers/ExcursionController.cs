using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Licenses;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Data.Enums;
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace Oblak.Controllers
{
    public class ExcursionController : Controller
    {
        private readonly ILogger<ExcursionController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;
        private HttpContext _context;

        public ExcursionController(
            ILogger<ExcursionController> logger, 
            ApplicationDbContext db, 
            IMapper mapper,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _logger = logger;            
            _db = db;
            _mapper = mapper;
            _context = httpContextAccessor.HttpContext!;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
                _legalEntityId = _appUser.LegalEntityId;
                _legalEntity = _appUser.LegalEntity;
            }
        }

        [HttpGet("excursions")]
        public async Task<IActionResult> Excursions()
        {
            return View();
        }

        [HttpGet("agency-excursions")]
        public async Task<IActionResult> Index(int agency)
        {
            ViewBag.Agency = agency;
            return PartialView();
        }

        private async Task<List<ExcursionDto>> GetData(int? AgencyId)
        {
			return await _db.Excursions.Include(a => a.Agency).Where(x => x.AgencyId == AgencyId || (AgencyId ?? 0) == 0)
				.Select(a => new ExcursionDto()
				{
					Id = a.Id,
					AgencyId = a.AgencyId,
					AgencyName = a.Agency.Name,
					Date = a.Date,
					VoucherNo = a.VoucherNo,
					NoOfPersons = a.NoOfPersons,
					ExcursionTaxAmount = a.ExcursionTaxAmount,
					ExcursionTaxExempt = a.ExcursionTaxExempt,
                    ExcursionTaxTotal = a.ExcursionTaxAmount * a.NoOfPersons,
					UserCreated = a.UserCreated,
					UserCreatedDate = a.UserCreatedDate,
					UserModified = a.UserModified,
					UserModifiedDate = a.UserModifiedDate,
				}).ToListAsync();
		}

        [HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request, [FromQuery] int? AgencyId)
        {
            var excursions = _db.Excursions.Include(a => a.Agency).Include(a => a.Country).Where(x => x.AgencyId == AgencyId || (AgencyId ?? 0) == 0)
                .Select(_mapper.Map<ExcursionDto>);
                //.ToList();

            //var excursions = await GetData(AgencyId);
            return Json(await excursions.ToDataSourceResultAsync(request));
        }


        [HttpPost]
        public async Task<ActionResult> Update(ExcursionDto dto, [DataSourceRequest] DataSourceRequest request, [FromQuery] int? AgencyId)
        {
            try
            {
                var exc = await _db.Excursions.FindAsync(dto.Id);

                if (exc == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(dto, exc);

                await _db.SaveChangesAsync();

                return Json(new[] { _mapper.Map(exc, dto) }.ToDataSourceResult(request, ModelState));

                var excursions = _db.Excursions.Include(a => a.Agency).Include(a => a.Country).Where(x => x.AgencyId == AgencyId || (AgencyId ?? 0) == 0)
                    .Select(_mapper.Map<ExcursionDto>);

                return Json(await excursions.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(ExcursionDto dto, [DataSourceRequest] DataSourceRequest request, [FromQuery] int? AgencyId)
        {
            try
            {
                var username = _context!.User!.Identity!.Name;
                var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

                var exc = new Excursion();
                _mapper.Map(dto, exc);
                exc.AgencyId = AgencyId.Value;
                _db.Add(exc);
                await _db.SaveChangesAsync();

                _db.Entry(exc).Reference(a => a.Country).Load();
				_db.Entry(exc).Reference(a => a.Agency).Load();

				return Json(new[] { _mapper.Map(exc, dto) }.ToDataSourceResult(request, ModelState));


                var excursions = _db.Excursions.Include(a => a.Agency).Include(a => a.Country).Where(x => x.AgencyId == AgencyId || (AgencyId ?? 0) == 0)
                    .Select(_mapper.Map<ExcursionDto>);
                return Json(await excursions.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, ExcursionDto dto, [FromQuery] int? AgencyId)
        {
            try
            {
                var exc = _db.NauticalTax.Find(dto.Id);                    

                if (exc != null)
                {                    
                    _db.NauticalTax.Remove(exc);
                    _db.SaveChanges();

                    return Json(new[] { _mapper.Map(exc, dto) }.ToDataSourceResult(request, ModelState));

                    var excursions = _db.Excursions.Include(a => a.Agency).Include(a => a.Country).Where(x => x.AgencyId == AgencyId || (AgencyId ?? 0) == 0)
                        .Select(_mapper.Map<ExcursionDto>);
                    return Json(await excursions.ToDataSourceResultAsync(request));
                }
                else
                {
                    return Json(new { error = "Izlet nije pronađen." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja izleta." });
            }
        }
    }
}