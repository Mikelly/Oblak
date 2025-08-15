using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using RegBor.Controllers;

namespace Oblak.Controllers
{
    public class FiscalController : Controller
    {

        private readonly ApplicationDbContext _db;
        private readonly ILogger<FiscalController> _logger;
        private readonly ApplicationUser _user;
        private readonly int _legalEntityId;


        public FiscalController(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext db,
            ILogger<FiscalController> logger
            )
        {
            _db = db;
            _logger = logger;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _user = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
                _legalEntityId = _user.LegalEntityId;
            }
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        [Route("fiscal-enu")]
        public IActionResult FiscalEnu(int property)
        {
            var prop = _db.Properties.FirstOrDefault(a => a.Id == property);
            var enu = _db.FiscalEnu.FirstOrDefault(a => a.PropertyId == property);

            var model = new FiscalDto()
            {
                Id = enu?.Id ?? 0,
                FiscalEnuCode = enu?.FiscalEnuCode,
                OperatorCode = enu?.OperatorCode,
                AutoDeposit = (double?)enu?.AutoDeposit,
                BusinessUnitCode = prop?.BusinessUnitCode,
                PropertyId = property
            };

            return PartialView(model);
        }


        [HttpPost]
        [Route("fiscal-enu")]
        public IActionResult FiscalEnu(FiscalDto model)
        {
            var prop = _db.Properties.FirstOrDefault(a => a.Id == model.PropertyId);
            var enu = _db.FiscalEnu.FirstOrDefault(a => a.Id == model.Id);

            try
            {
                if (enu == null)
                {
                    enu = new FiscalEnu();
                    enu.PropertyId = model.PropertyId;
                    enu.LegalEntityId = _legalEntityId;
                    _db.FiscalEnu.Add(enu);
                }

                enu.OperatorCode = model.OperatorCode;
                enu.AutoDeposit = (decimal?)model.AutoDeposit;
                enu.FiscalEnuCode = model.FiscalEnuCode;
                prop.BusinessUnitCode = model.BusinessUnitCode;

                _db.SaveChanges();

                return Json(new BasicDto() { error = "", info = "OK" });
            }
            catch (Exception ex)
            {
                return Json(new BasicDto() { error = ex.Message, info = "" });
            }
        }

        [HttpGet("fiscal-enu-all")]
        public async Task<IActionResult> FiscalEnuAll(int legalEntityId)
        {
            if (_legalEntityId <= 0)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Json(new BasicDto { error = "Korisnik nije ulogovan!" });
            } 

            var exists = await _db.LegalEntities.AnyAsync(le => le.Id == legalEntityId);
            if (!exists)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new BasicDto { error = "Korisnik nije pronadjen!" });
            }

            try
            { 
                var result = await _db.Properties
                    .Where(p => p.LegalEntityId == legalEntityId)
                    .GroupJoin(
                        _db.FiscalEnu,
                        prop => prop.Id,
                        fe => fe.PropertyId,
                        (prop, feGroup) => new FiscalEnuAllDto
                        {
                            PropertyId = prop.Id,
                            Codes = feGroup
                                .Select(f => f.FiscalEnuCode)
                                .ToList()
                        }
                    )
                    .ToListAsync();
                  
                return Json(result);
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "Greska pri citanju fiscal ENU codes za LegalEntityId {LegalEntityId}", legalEntityId);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(new BasicDto { id = legalEntityId, error = "Došlo je do interne greške." });
            }
        }

        [HttpGet("fiscal-enu-all-pe")]
        public async Task<IActionResult> FiscalEnuAllPe(int legalEntityId)
        {
            if (_legalEntityId <= 0)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Json(new BasicDto { error = "Korisnik nije ulogovan!" });
            }

            var exists = await _db.LegalEntities.AnyAsync(le => le.Id == legalEntityId);
            if (!exists)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Json(new BasicDto { error = "Korisnik nije pronađen!" });
            }

            try
            {
                var result = await _db.Properties
                    .Where(p => p.LegalEntityId == legalEntityId)
                    .Join(
                        _db.FiscalEnu,
                        prop => prop.Id,
                        fe => fe.PropertyId,
                        (prop, fe) => new
                        {
                            PropertyId = prop.Id,
                            EnuCode = fe.FiscalEnuCode
                        }
                    )
                    .ToListAsync();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri čitanju fiscal ENU codes za LegalEntityId {LegalEntityId}", legalEntityId);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(new BasicDto { id = legalEntityId, error = "Došlo je do interne greške." });
            }
        }

    }

}