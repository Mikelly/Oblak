using AutoMapper;
using DocumentFormat.OpenXml.VariantTypes;
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
using Telerik.SvgIcons;

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

            var partner = _db.Partners.FirstOrDefault(a => a.Id == _appUser.PartnerId);
            var errInfo = string.Empty;

            if (partner.CheckRegistered)
            {
                var le = 0;

                if (legalEntity.HasValue)
                {
                    le = legalEntity.Value;
                }
                else if (Property.HasValue)
                {
                    var prop = _db.Properties.FirstOrDefault(a => a.Id == Property);
                    le = prop!.LegalEntityId;
                    if (prop.RegDate != null)
                    { 
                        var date = prop.RegDate.Value;
                        var diff = date - DateTime.Now.Date;
                        if(diff.TotalDays <= 30 && diff.TotalDays > 0)
                        {
                            errInfo = $"Rješenje ističe za {diff.TotalDays.ToString("0")} dana, tj. {date.ToString("dd.MM.yyyy")}";
                        }
                    }
                }
                else
                {

                }

                var balance = _db.TaxPaymentBalances.Where(a => a.TaxType == TaxType.ResidenceTax && a.LegalEntityId == le).FirstOrDefault();

                var all = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId).Select(a => a.Id).ToList();
                var unpaid = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.PaymentStatus == TaxPaymentStatus.Unpaid).Select(a => a.Id).ToList();
                var advance = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.PaymentStatus == TaxPaymentStatus.PaidInAdvance).Select(a => a.Id).ToList();

                var not_registered = _db.Properties.Where(a => a.LegalEntityId == le).Any(a => a.RegDate == null || a.RegDate < DateTime.Now.Date);

                if (balance?.Balance > 0 && not_registered == false)
                {
                    return Json(new { allowed = true, not_registered, errInfo, Ids = new List<int>() });
                }
                if (balance?.Balance <= 0 && not_registered == false)
                {
                    return Json(new { allowed = true, not_registered, errInfo, Ids = advance });
                }
                else
                {
                    return Json(new { allowed = false, not_registered, errInfo, Ids = unpaid.Union(advance).ToList() });
                }
            }
            else
            {
                return Json(new { allowed = true, registered = true, errInfo, Ids = new List<int>() });
            }
        }


        [HttpGet("payments", Name = "Payments")]
        public ActionResult Payments(int? legalEntity, int? property, int? agency)
        {
            //var calc = _db.GetBalance("ReseidenceTax", legalEntity, 0);

            var partner = _db.Partners.FirstOrDefault(a => a.Id == _appUser.PartnerId);
            var errInfo = string.Empty;
            var all = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId).Select(a => new { Value = a.Id.ToString(), Text = a.Description }).ToList();

            if (partner.CheckRegistered)
            {
                var le = 0;

                if (legalEntity.HasValue)
                {
                    le = legalEntity.Value;
                }
                else if (property.HasValue)
                {
                    var prop = _db.Properties.FirstOrDefault(a => a.Id == property);
                    le = prop!.LegalEntityId;
                    if (prop.RegDate != null)
                    {
                        var date = prop.RegDate.Value;
                        var diff = date - DateTime.Now.Date;
                        if (diff.TotalDays <= 30 && diff.TotalDays > 0)
                        {
                            errInfo = $"Rješenje ističe za {diff.TotalDays.ToString("0")} dana, tj. {date.ToString("dd.MM.yyyy")}";
                        }
                    }
                }
                else
                {

                }

                var balance = _db.TaxPaymentBalances.Where(a => a.TaxType == TaxType.ResidenceTax && a.LegalEntityId == le).FirstOrDefault();

                
                var unpaid = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.PaymentStatus == TaxPaymentStatus.Unpaid).Select(a => new { Value = a.Id.ToString(), Text = a.Description }).ToList();
                var advance = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.PaymentStatus == TaxPaymentStatus.PaidInAdvance).Select(a => new { Value = a.Id.ToString(), Text = a.Description }).ToList();
                
                var not_registered = _db.Properties.Where(a => a.LegalEntityId == le).Any(a => a.RegDate == null || a.RegDate < DateTime.Now.Date);

                if (_appUser.PartnerId == 3)
                {
                    return Json(new { allowed = true, not_registered, errInfo, Ids = new List<int>(), payments = all });
                }

                if ((balance?.Balance > 0 || balance == null) && not_registered == false)
                {
                    return Json(new { allowed = true, not_registered, errInfo, Ids = new List<int>(), payments = all });
                }
                if ((balance?.Balance <= 0 || balance == null) && not_registered == false)
                {
                    return Json(new { allowed = true, not_registered, errInfo, Ids = advance, payments = all.Except(advance) });
                }
                else
                {
                    return Json(new { allowed = false, not_registered, errInfo, Ids = unpaid.Union(advance).ToList(), payments = all.Except(unpaid).Except(advance) });
                }
            }
            else
            {
                return Json(new { allowed = true, registered = true, errInfo, Ids = new List<int>(), payments = all });
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