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
    public class TaxPaymentBalanceController : Controller
    {
        private readonly ILogger<TaxPaymentBalanceController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;

        public TaxPaymentBalanceController(
            ILogger<TaxPaymentBalanceController> logger, 
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
        [Route("legal-entity-balance")]
        public async Task<IActionResult> Index(int le)
        {
            ViewBag.le = le;            
            return PartialView();
        }

        [HttpGet("deferred", Name = "Deferred")]
        public ActionResult Deferred(int? legalEntity, int? Property, int? agency)
        {
            //var calc = _db.GetBalance("ReseidenceTax", legalEntity, 0);

            var le = 0;

            if (legalEntity.HasValue)
            {
                le = legalEntity.Value;
            }
            else if (Property.HasValue)
            {
                le = _db.Properties.FirstOrDefault(a => a.Id == Property)!.LegalEntityId;
            }
            else
            { 
            
            }
            
            var balance = _db.TaxPaymentBalances.Where(a => a.TaxType == TaxType.ResidenceTax && a.LegalEntityId == le).FirstOrDefault();

            if (balance?.Balance > 0)
            {
                return Json(new { allowed = true, Id = 4 });
            }
            else
            {
                return Json(new { allowed = false, Id = 4 });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request, int le)
        {            
            var balance = _db.TaxPaymentBalances.Where(a => a.LegalEntityId == le);
            var data = _mapper.Map<List<TaxPaymentBalanceDto>>(balance);
            return Json(await data.ToDataSourceResultAsync(request));
        }
    }
}