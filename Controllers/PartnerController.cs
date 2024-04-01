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
using System.Security.Cryptography;

namespace Oblak.Controllers
{
    public class PartnerController : Controller
    {
        private readonly ILogger<PartnerController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;

        public PartnerController(
            ILogger<PartnerController> logger, 
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
        [Route("partners")]
        public async Task<IActionResult> Index()
        {
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {            
            var checkpoints = _db.Partners;
            var data = _mapper.Map<List<PartnerDto>>(checkpoints);
            return Json(await data.ToDataSourceResultAsync(request));
        }


        [HttpPost]
        public async Task<ActionResult> Update(PartnerDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var prtn = await _db.Partners.FindAsync(dto.Id);

                if (prtn == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(dto, prtn);

                await _db.SaveChangesAsync();

                var checkpoints = _db.Partners;
                var data = _mapper.Map<List<PartnerDto>>(checkpoints);
                return Json(await data.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(PartnerDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var prtn = new Partner();
                _mapper.Map(dto, prtn);                
                _db.Partners.Add(prtn);
                await _db.SaveChangesAsync();

                var checkpoints = _db.Partners;
                var data = _mapper.Map<List<PartnerDto>>(checkpoints);
                return Json(await data.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, PartnerDto dto)
        {
            try
            {
                var prtn = _db.Partners.Find(dto.Id);                    

                if (prtn != null)
                {                    
                    _db.Partners.Remove(prtn);
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

        [HttpGet]
        [Route("upload-partner-logo", Name = "UploadPartnerLogoGet")]
        public IActionResult UploadLogo(int legalEntity, string hide)
        {
            var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
            ViewBag.LegalEntity = legalEntity;
            ViewBag.Logo = firma.Logo;
            ViewBag.Hide = hide == "Y";
            return PartialView();
        }

        [HttpPost]
        [Route("upload-partner-logo", Name = "UploadPartnerLogo")]
        public async Task<IActionResult> Uploadlogo(int partner, IFormFile logo)
        {
            try
            {
                var bytes = await logo.GetBytes();

                var prtn = _db.Partners.FirstOrDefault(a => a.Id == partner);
                if (prtn != null)
                {
                    prtn.Logo = bytes;
                    _db.SaveChanges();
                }
            }
            catch (CryptographicException ex)
            {
                return Json(new BasicDto() { info = "", error = "Nijeste odabrali odgovarajući fajl, ili je sertifikat neispravan!" });
            }

            return Json(new BasicDto() { info = "Logo uspješno sačuvan!", error = "" });
        }
    }
}