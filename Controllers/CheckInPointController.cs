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
    public class CheckInPointController : Controller
    {
        private readonly ILogger<CheckInPointController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;

        public CheckInPointController(
            ILogger<CheckInPointController> logger, 
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
            }
        }

        [HttpGet]
        [Route("check-in-points")]
        public async Task<IActionResult> Index()
        {
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {            
            var checkpoints = _db.CheckInPoints.Where(a => a.PartnerId == _legalEntity.PartnerId);
            var data = _mapper.Map<List<CheckInPointDto>>(checkpoints);
            return Json(await data.ToDataSourceResultAsync(request));
        }


        [HttpPost]
        public async Task<ActionResult> Update(CheckInPointDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var cp = await _db.CheckInPoints.FindAsync(dto.Id);

                if (cp == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(dto, cp);

                await _db.SaveChangesAsync();

                var checkpoints = _db.CheckInPoints.Where(a => a.PartnerId == _legalEntity.PartnerId);
                var data = _mapper.Map<List<CheckInPointDto>>(checkpoints);
                return Json(await data.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(CheckInPointDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var cp = new CheckInPoint();
                _mapper.Map(dto, cp);
                cp.PartnerId = _legalEntity.PartnerId ?? 1;                
                _db.Add(cp);
                await _db.SaveChangesAsync();

                var checkpoints = _db.CheckInPoints.Where(a => a.PartnerId == _legalEntity.PartnerId);
                var data = _mapper.Map<List<CheckInPointDto>>(checkpoints);
                return Json(await data.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, CheckInPointDto dto)
        {
            try
            {
                var cp = _db.CheckInPoints.Find(dto.Id);                    

                if (cp != null)
                {                    
                    _db.CheckInPoints.Remove(cp);
                    _db.SaveChanges();
                    var checkpoints = _db.CheckInPoints.Where(a => a.PartnerId == _legalEntity.PartnerId);
                    var data = _mapper.Map<List<CheckInPointDto>>(checkpoints);
                    return Json(await data.ToDataSourceResultAsync(request));                    
                }
                else
                {
                    return Json(new { error = "Punkt nije pronađen." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja punkta." });
            }
        }
    }
}