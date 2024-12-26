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
using System.Linq;

namespace Oblak.Controllers
{
    public class ExcursionInvoiceItemController : Controller
    {
        private readonly ILogger<ExcursionInvoiceItemController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;
        private HttpContext _context;

        public ExcursionInvoiceItemController(
            ILogger<ExcursionInvoiceItemController> logger, 
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

        [HttpGet("excursion-invoice-items")]
        public async Task<IActionResult> Index(int? agency)
        {
            ViewBag.Agency = agency;
            return PartialView();
        }


        private decimal CalcTaxFee(decimal amount)
        {
            var type = _db.ResTaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.PaymentStatus == TaxPaymentStatus.Cash).FirstOrDefault();

            var result = _db.ResTaxFees.Where(a => a.ResTaxPaymentTypeId == type!.Id && a.PartnerId == _appUser.PartnerId)
                .Where(a => a.LowerLimit <= amount && amount <= a.UpperLimit)
                .FirstOrDefault();

            if (result != null)
            {
                if (result.FeeAmount.HasValue) return result.FeeAmount ?? 0m;
                else if (result.FeePercentage.HasValue) return amount * (result.FeePercentage ?? 0m) / 100m;
                else return 0;
            }
            else return 0;
        }

        [HttpGet("excursion-invoice-totals")]
        public async Task<IActionResult> Totals([FromQuery] int? InvoiceId)
        {
            var invoice = _db.ExcursionInvoices.FirstOrDefault(a => a.Id == InvoiceId);                

            return Json(new { Amount = invoice.BillingAmount, Fee = invoice.BillingFee, Total = invoice.BillingAmount + invoice.BillingFee });
        }


        [HttpPost("excursion-invoice-items-read")]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request, [FromQuery] int? InvoiceId)
        {                    
            var items = _db.ExcursionInvoiceItems.Include(a => a.Country)
                .Where(x => x.ExcursionInvoiceId == InvoiceId)
                .Select(_mapper.Map<ExcursionInvoiceItemDto>)
                .ToList();

            return Json(await items.ToDataSourceResultAsync(request));
        }


        [HttpPost]
        public async Task<ActionResult> Update(ExcursionInvoiceItemDto dto, [DataSourceRequest] DataSourceRequest request, int? InvoiceId)
        {
            try
            {
                var item = _db.ExcursionInvoiceItems.Include(a => a.ExcursionInvoice).FirstOrDefault(a => a.Id == dto.Id);

                if (item == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(dto, item);

                item.Amount = item.NoOfPersons * item.Price;

                await _db.SaveChangesAsync();

                var totalAmount = _db.ExcursionInvoiceItems.Where(a => a.ExcursionInvoiceId == InvoiceId).Select(a => a.Amount).Sum();
                item.ExcursionInvoice.BillingAmount = totalAmount;
                var fee = CalcTaxFee(totalAmount);
                item.ExcursionInvoice.BillingFee = fee;

                await _db.SaveChangesAsync();

                var items = _db.ExcursionInvoiceItems
                    .Where(x => x.ExcursionInvoiceId == InvoiceId)
                    .Select(_mapper.Map<ExcursionInvoiceItemDto>)
                    .ToList();

                return Json(new[] { _mapper.Map(item, dto) }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }


        [HttpPost]
        public async Task<ActionResult> Create(ExcursionInvoiceItemDto dto, [DataSourceRequest] DataSourceRequest request, int InvoiceId)
        {
            try
            {
                var username = _context!.User!.Identity!.Name;
                var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

                var item = new ExcursionInvoiceItem();
                _mapper.Map(dto, item);
                item.ExcursionInvoiceId = InvoiceId;
                item.Amount = item.NoOfPersons * item.Price;
                _db.Add(item);
                await _db.SaveChangesAsync();

                _db.Entry(item).Reference(a => a.ExcursionInvoice).Load();

                var totalAmount = _db.ExcursionInvoiceItems.Include(a => a.ExcursionInvoice).Include(a => a.ExcursionInvoice).Where(a => a.ExcursionInvoiceId == InvoiceId).Select(a => a.Amount).Sum();
                item.ExcursionInvoice.BillingAmount = totalAmount;
                var fee = CalcTaxFee(totalAmount);
                item.ExcursionInvoice.BillingFee = fee;

                await _db.SaveChangesAsync();

                return Json(new[] { _mapper.Map(item, dto) }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, ExcursionInvoiceItemDto dto, int InvoiceId)
        {
            try
            {
                var item = _db.ExcursionInvoiceItems.Find(dto.Id);

                if (item != null)
                {
                    _db.ExcursionInvoiceItems.Remove(item);
                    _db.SaveChanges();

                    return Json(new[] { dto }.ToDataSourceResult(request, ModelState));
                }
                else
                {
                    return Json(new { error = "Stavka nije pronađena." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja stavke." });
            }
        }

    }
}