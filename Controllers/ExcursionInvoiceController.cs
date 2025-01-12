using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Licenses;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Data.Enums;
using Oblak.Migrations;
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.Reporting;
using Oblak.Services.SRB;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Oblak.Controllers
{
    public class ExcursionInvoiceController : Controller
    {
        private readonly ILogger<ExcursionInvoiceController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ReportingService _reporting;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;
        private HttpContext _context;

        public ExcursionInvoiceController(
            ILogger<ExcursionInvoiceController> logger, 
            ApplicationDbContext db, 
            ReportingService reporting,
            IWebHostEnvironment env,
            IMapper mapper,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _logger = logger;       
            _reporting = reporting;
            _env = env;
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

        [HttpGet("excursion-invoices")]
        public async Task<IActionResult> Index(int? agency)
        {
            ViewBag.Agency = agency;
            return PartialView();
        }


        [HttpPost("excursion-invoices-read")]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request, [FromQuery] int? AgencyId)
        {
            var invoices = _db.ExcursionInvoices
                .Include(a => a.Agency)
                .Include(a => a.TaxPaymentType)
                .Where(x => x.AgencyId == AgencyId || (AgencyId ?? 0) == 0)
                .Select(_mapper.Map<ExcursionInvoiceDto>);                

            return Json(await invoices.ToDataSourceResultAsync(request));
        }


        [HttpGet("excursion-invoice")]
        public async Task<IActionResult> Invoice(int Id)
        {
            var sl = _db.TaxPaymentTypes
                .Where(a => a.PartnerId == _appUser.PartnerId)
                .Select(a =>
                    new SelectListItem() { Text = a.Description, Value = a.Id.ToString() }
                ).ToList();

            ViewBag.TaxPaymentTypes = new SelectList(sl, "Value", "Text");

            ViewBag.Invoice = _mapper.Map<ExcursionInvoiceDto>(_db.ExcursionInvoices.Include(a => a.Agency).Where(a => a.Id == Id).Include(a => a.TaxPaymentType).FirstOrDefault());

            ViewBag.InvoiceId = Id;
            return View("Invoice");
        }


        [HttpGet("excursion-invoice-create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var dto = new ExcursionInvoiceDto();

                dto.Date = DateTime.Now;
                dto.TaxPaymentTypeId = 2;
                dto.InvoiceNo = _db.ExcursionInvoices.Where(a => a.Date.Year == DateTime.Now.Year).Select(a => a.InvoiceNo).OrderByDescending(x => x).FirstOrDefault() + 1;
                dto.InvoiceNumber = $"{dto.InvoiceNo.ToString("0")}/{DateTime.Now.Year.ToString("0")}";
                dto.Status = "U izradi";

                var sl = _db.TaxPaymentTypes
                    .Where(a => a.PartnerId == _appUser.PartnerId)
                    .Select(a =>
                        new SelectListItem() { Text = a.Description, Value = a.Id.ToString() }
                    ).ToList();

                ViewBag.TaxPaymentTypes = new SelectList(sl, "Value", "Text");

                ViewBag.Invoice = dto;

                ViewBag.InvoiceId = 0;

                return View("Invoice");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("excursion-invoice-save")]
        public async Task<IActionResult> Save(ExcursionInvoiceDto dto)
        {
            try
            {
                ExcursionInvoice invoice = null;

                if (dto.Id == 0)
                {
                    invoice = new ExcursionInvoice();
                    invoice.UserCreated = User.Identity.Name;
                    invoice.UserCreatedDate = DateTime.Now;
                    _db.ExcursionInvoices.Add(invoice);
                }
                else
                {
                    invoice = _db.ExcursionInvoices.FirstOrDefault(a => a.Id == dto.Id)!;
                }

                _mapper.Map(dto, invoice);

                invoice.CheckInPointId = _appUser.CheckInPointId ?? 0;
                invoice.DueDate = invoice.Date.AddDays(15);

                if (invoice.AgencyId == 0)
                {
                    return Ok(new { id = invoice.Id, info = "", error = "Morate unijeti agenciju!" });
                }

                _db.SaveChanges();

                return Ok(new { id = invoice.Id, info = "OK", error = "" });
            }
            catch (Exception ex)
            {
                return Ok(new { id = 0, info = "", error = ex.Message });                
            }
        }


        [HttpPost("excursion-invoice-delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                ExcursionInvoice invoice = _db.ExcursionInvoices.FirstOrDefault(a => a.Id == id)!;

                if (invoice.Status == TaxInvoiceStatus.Active || invoice.Status == TaxInvoiceStatus.Opened)
                {
                    _db.Remove(invoice);
                    _db.SaveChanges();

                    return Ok(new { id = invoice.Id, info = "Uspješno obrisana faktura", error = "" });
                }
                else
                {
                    return Ok(new { id = invoice.Id, info = "Ne možete brisati zaključenu fakturu", error = "" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { id = 0, info = "", error = ex.Message });
            }
        }


        [HttpGet("excursion-invoice-pdf")]
        public async Task<FileContentResult> InvoicePdf(int invoice)
        {
            var toReport = $"{_appUser.PartnerId}ExcursionInvoice";
            var path = Path.Combine(_env.ContentRootPath, "Reports", toReport);

            var bytes = _reporting.RenderReport(
            path,
                new List<Telerik.Reporting.Parameter>() {
                    new Telerik.Reporting.Parameter(){ Name = "Invoice", Value = invoice }
                },
                "PDF");

            return File(bytes, "application/pdf");
        }

        [HttpGet("generate-invoice")]
        public async Task<JsonResult> Generate(int invoiceId)
        {
            try
            {
                var inv = _db.ExcursionInvoices.Include(a => a.TaxPaymentType).FirstOrDefault(a => a.Id == invoiceId);
                var dateFrom = inv.BillingPeriodFrom;
                var dateTo = inv.BillingPeriodTo;

                if (dateFrom == null || dateTo == null)
                {
                    return Json(new { info = "", error = "Morate unijeti period od kada unosite izlete u fakturu!" });
                }

                var exursions = _db.Excursions.Where(a => a.AgencyId == inv.AgencyId && a.Date >= dateFrom.Value && a.Date <= dateTo.Value).ToList();

                foreach (var e in exursions.GroupBy(a => new { a.Date, a.VoucherNo, a.ExcursionTaxAmount, a.CountryId }))
                {
                    var ei = new ExcursionInvoiceItem();
                    ei.ExcursionInvoiceId = invoiceId;
                    ei.Price = e.Key.ExcursionTaxAmount;
                    ei.VoucherNo = e.Key.VoucherNo;
                    ei.Date = e.Key.Date;
                    ei.CountryId = e.Key.CountryId;
                    ei.NoOfPersons = e.Sum(a => a.NoOfPersons);
                    ei.Amount = ei.NoOfPersons * ei.Price;
                    _db.ExcursionInvoiceItems.Add(ei);
                }
                _db.SaveChanges();

                var totalAmount = _db.ExcursionInvoiceItems.Where(a => a.ExcursionInvoiceId == inv.Id).Select(a => a.Amount).Sum();
                inv.BillingAmount = totalAmount;
                if (inv.TaxPaymentType.IsCash)
                {
                    var fee = CalcTaxFee(totalAmount);
                    inv.BillingFee = fee;
                }
                else
                {
                    inv.BillingFee = 0;
                }

                _db.SaveChanges();

                return Json(new { info = "Uspješno uneseni izleti u fakturu!", error = "" });

            }
            catch (Exception ex)
            {
                return Json(new { info = "", error = ex.Message });
            }
        }

        [HttpGet("excursion-invoice-close")]
        public async Task<IActionResult> Close(int invoiceId)
        {
            try
            {
                var inv = _db.ExcursionInvoices.FirstOrDefault(a => a.Id == invoiceId);
                inv.Status = TaxInvoiceStatus.Closed;
                
                _db.SaveChanges();

                return Json(new { info = "Uspješno zaključena faktura!", error = "" });
            }
            catch (Exception ex)
            {
                return Json(new { info = "", error = ex.Message });
            }
        }

        [HttpGet("excursion-invoice-open")]
        public async Task<IActionResult> Open(int invoiceId)
        {
            try
            {
                var inv = _db.ExcursionInvoices.FirstOrDefault(a => a.Id == invoiceId);
                inv.Status = TaxInvoiceStatus.Opened;

                _db.SaveChanges();

                return await Invoice(invoiceId);
            }
            catch (Exception ex)
            {
                return Json(new { info = "", error = ex.Message });
            }
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
    }
}