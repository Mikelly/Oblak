using AutoMapper;
using DocumentFormat.OpenXml.InkML;
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
using System.Data;
using Microsoft.Data.SqlClient;
using Oblak.Helpers;
using Microsoft.OpenApi.Writers;

namespace Oblak.Controllers
{
    public class TaxPaymentController : Controller
    {
        private readonly ILogger<TaxPaymentController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;

        public TaxPaymentController(
            ILogger<TaxPaymentController> logger, 
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
        [Route("legal-entity-payments")]
        public async Task<IActionResult> Index(int? le, int? ag, string taxType)
        {
			ViewBag.le = le;
            ViewBag.ag = ag; 
            ViewBag.taxType = taxType;

            var prtn = _appUser.PartnerId;

            var paymethods = _db.TaxPaymentTypes.Where(a => a.PartnerId == prtn).Select(a => new SelectListItem()
            {
                Text = a.Description,
                Value = a.Id.ToString()
            }).ToList();

            ViewBag.CashPaymentIds = _db.TaxPaymentTypes.Where(a => a.PartnerId == _appUser.PartnerId && a.TaxPaymentStatus == TaxPaymentStatus.Cash).Select(a => a.Id).ToArray();

            ViewBag.PaymentMethods = new SelectList(paymethods, "Value", "Text");

			var balance = _db.TaxPaymentBalances.Where(a => a.LegalEntityId == le && a.TaxType == TaxType.ResidenceTax).FirstOrDefault();
			if (balance == null)
			{
				balance = new TaxPaymentBalance();
				balance.LegalEntityId = le;
				balance.AgencyId = ag;
				balance.TaxType = TaxType.ResidenceTax;
				_db.Add(balance);
				await _db.SaveChangesAsync();
			}

			var calc = _db.GetBalance("ResidenceTax", le, 0);
			balance.Balance = calc;
			await _db.SaveChangesAsync();

			ViewBag.Balance = _db.TaxPaymentBalances.Where(a => a.TaxType == TaxType.ResidenceTax && a.LegalEntityId == le).Select(a => a.Balance).FirstOrDefault();

			return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request, int? le, int? ag, string taxType)
        {      
            var tt = (TaxType)Enum.Parse(typeof(TaxType), taxType);
            var payments = _db.TaxPayments.Where(a => (a.LegalEntityId == (le ?? -1) || a.AgencyId == (ag ?? -1)) && a.TaxType == tt);
            var data = _mapper.Map<List<TaxPaymentDto>>(payments);
            return Json(await data.ToDataSourceResultAsync(request));
        }


        [HttpPost]
        public async Task<ActionResult> Update(TaxPaymentDto dto, [DataSourceRequest] DataSourceRequest request, int? le, int? ag, [FromQuery] string taxType)
        {
            try
            {
                if (User.IsInRole("TouristOrgAdmin") == false && User.IsInRole("TouristOrgControllor") == false)
                {
                    return Unauthorized();
                }

                var payment = await _db.TaxPayments.FindAsync(dto.Id);

                if (payment == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                if (_db.TaxPayments.Any(a => a.Reference == dto.Reference && a.Id != payment.Id))
                {
                    return Json(new DataSourceResult { Errors = "Uplatnica je već iskorišćena." });
                }

                _mapper.Map(dto, payment);

                var tt = (TaxType)Enum.Parse(typeof(TaxType), taxType);
                payment.TaxType = tt;

                payment.AgencyId = ag;
                payment.LegalEntityId = le;

                payment.UserModified = User.Identity.Name;
                payment.UserModifiedDate = DateTime.Now;

                await _db.SaveChangesAsync();
                
                var payments = _db.TaxPayments.Where(a => (a.LegalEntityId == (le ?? -1) || a.AgencyId == (ag ?? -1)) && a.TaxType == tt);
                var data = _mapper.Map<List<TaxPaymentDto>>(payments);
				return Json(await data.ToDataSourceResultAsync(request));
			}
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(TaxPaymentDto dto, [DataSourceRequest] DataSourceRequest request, int? le, int? ag, [FromQuery] string taxType)
        {
            try
            {
                var pay = new TaxPayment();

                //_mapper.Map(dto, pay);

                if (_db.TaxPayments.Any(a => a.Reference == dto.Reference))
                {
                    return Json(new DataSourceResult { Errors = "Uplatnica je već iskorišćena." });
                }

                pay.Amount = dto.Amount ?? 0;
                pay.TransactionDate = dto.TransactionDate ?? DateTime.Now;
                pay.Note = dto.Note;
                pay.Reference = dto.Reference;
                pay.LegalEntityId = le;
                pay.AgencyId = ag;
                pay.CheckInPointId = _appUser.CheckInPointId;
                pay.TaxPaymentTypeId = dto.TaxPaymentTypeId;

                var tt = (TaxType)Enum.Parse(typeof(TaxType), taxType);
                pay.TaxType = tt;

                pay.UserCreated = User.Identity.Name;
                pay.UserCreatedDate = DateTime.Now;

                pay.UserModified = User.Identity.Name;
                pay.UserModifiedDate = DateTime.Now;

                _db.Add(pay);

                await _db.SaveChangesAsync();                

                var balance = _db.TaxPaymentBalances.Where(a => (a.LegalEntityId == (le ?? -1) || a.AgencyId == (ag ?? -1)) && a.TaxType == tt).FirstOrDefault();
                if (balance == null)
                { 
                    balance = new TaxPaymentBalance();
                    balance.LegalEntityId = le;
                    balance.AgencyId = ag;
                    balance.TaxType = tt;
                    _db.Add(balance);   
                    await _db.SaveChangesAsync();   
                }

                var calc = _db.GetBalance("ResidenceTax", le ?? 0, ag ?? 0);

                balance.Balance = calc;
                await _db.SaveChangesAsync();

                ViewBag.Balance = calc;

                var payments = _db.TaxPayments.Where(a => (a.LegalEntityId == (le ?? -1) || a.AgencyId == (ag ?? -1)) && a.TaxType == tt);
                var data = _mapper.Map<List<TaxPaymentDto>>(payments);
				return Json(await data.ToDataSourceResultAsync(request));
			}
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, TaxPaymentDto dto, int? le, int? ag, [FromQuery] string taxType)
        {
            try
            {
                if (User.IsInRole("TouristOrgAdmin") == false && User.IsInRole("TouristOrgControllor") == false)
                {
                    return Unauthorized();
                }

                var pay = _db.TaxPayments.Find(dto.Id);                    

                if (pay != null)
                {                    
                    _db.TaxPayments.Remove(pay);
                    _db.SaveChanges();

                    var tt = (TaxType)Enum.Parse(typeof(TaxType), taxType);
                    var payments = _db.TaxPayments.Where(a => (a.LegalEntityId == (le ?? -1) || a.AgencyId == (ag ?? -1)) && a.TaxType == tt);
                    var data = _mapper.Map<List<TaxPaymentDto>>(payments);
					return Json(await data.ToDataSourceResultAsync(request));
				}
                else
                {
                    return Json(new { error = "Uplata nije pronađena." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja uplate." });
            }
        }


        [HttpGet("ext-pay")]
        public async Task<ActionResult> ExtPay(int? payid, int? p, int? g, int? inv)
        {
            try
            {
                var person = _db.MnePersons.FirstOrDefault(a => a.Id == p);
                var group = _db.Groups.FirstOrDefault(a => a.Id == g);
                var invoice = _db.ExcursionInvoices.FirstOrDefault(a => a.Id == inv);

                TaxPayment tp = null;

                int? le = null;

                if (g.HasValue)
                {
                    tp = _db.TaxPayments.Where(a => a.GroupId == g).FirstOrDefault();
                }
                else if (p.HasValue)
                {
                    tp = _db.TaxPayments.Where(a => a.PersonId == p).FirstOrDefault();                    
                }
                else if (inv.HasValue)
                {
                    tp = _db.TaxPayments.Where(a => a.InvoiceId == inv).FirstOrDefault();
                }                

                var dto = new TaxPaymentDto();

                ViewBag.Enable = true;

                if (tp != null)
                {
                    dto = _mapper.Map<TaxPaymentDto>(tp);
                    ViewBag.Amount = tp.Amount;
                    if (person != null)
                    {
                        ViewBag.Enable = person.ResTaxStatus == ResTaxStatus.Open;
                    }
                }
                else
                {
                    if (g.HasValue)
                    {
                        dto.GroupId = g;
                    }
                    else if (p.HasValue)
                    {
                        dto.PersonId = p;                        
                    }
                    else if (inv.HasValue)
                    {
                        dto.InvoiceId = inv;
                    }
                    ViewBag.Amount = group?.ResTaxAmount ?? person?.ResTaxAmount ?? invoice?.BillingAmount;
                }

                ViewBag.Dto = dto;

                if (User.IsInRole("TouristOrgAdmin"))
                { 
                    ViewBag.Enable = true;
                }

                //var sl = _db.TaxPaymentTypes
                //    .Where(a => a.PartnerId == _appUser.PartnerId)
                //    .Select(a =>
                //        new SelectListItem() { Text = a.Description, Value = a.Id.ToString() }
                //    ).ToList();

                //ViewBag.PaymentTypes = new SelectList(sl, "Value", "Text");

                //if(User.IsInRole("TouristOrgCopntrollor") || User.IsInRole("TouristOrgAdmin"))
                //{
                //    ViewBag.Enable = true;
                //}

                return PartialView();
                
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške." });
            }
        }

        [HttpPost("do-ext-pay")]
        public async Task<ActionResult> DoExtPay(TaxPaymentDto dto)
        {
            try
            {
                int.TryParse(Request.Form["PayId"].ToString(), out int id);

                TaxPayment pay = null;

                int? le = null;
                int? ag = null;

                if (dto.PersonId.HasValue)
                {
                    pay = _db.TaxPayments.Where(a => a.PersonId == dto.PersonId).FirstOrDefault();
                    var p = _db.MnePersons.Include(a => a.Property).FirstOrDefault(a => a.Id == dto.PersonId);
                    le = p.Property.LegalEntityId;
                }
                else if (dto.GroupId.HasValue)
                {
                    pay = _db.TaxPayments.Where(a => a.GroupId == dto.GroupId).FirstOrDefault();
                    var g = _db.Groups.Include(a => a.Property).FirstOrDefault(a => a.Id == dto.GroupId);
                    le = g.Property.LegalEntityId;
                }
                else if (dto.InvoiceId.HasValue)
                {
                    pay = _db.TaxPayments.Where(a => a.InvoiceId == dto.InvoiceId).FirstOrDefault();
                    var i = _db.ExcursionInvoices.FirstOrDefault(a => a.Id == dto.InvoiceId);
                    ag = i.AgencyId;
                }

                if (pay == null)
                {
                    var exists = _db.TaxPayments.Any(a => a.Reference == dto.Reference);
                    if (exists) return Json(new { error = "Broj uplatnice već postoji!.", info = "" });

                    pay = new TaxPayment();
                    _db.TaxPayments.Add(pay);
                }
                else
                {
					var exists = _db.TaxPayments.Any(a => a.Reference == dto.Reference && a.Id != pay.Id);
					if (exists) return Json(new { error = "Broj uplatnice već postoji!.", info = "" });
				}    

                pay.Amount = dto.Amount ?? 0;
                pay.PersonId = dto.PersonId;
                pay.GroupId = dto.GroupId;
                pay.Reference = dto.Reference;
                pay.LegalEntityId = le;
                pay.AgencyId = ag;
                pay.TransactionDate = dto.TransactionDate ?? DateTime.Now;
                pay.TaxPaymentTypeId = _db.TaxPaymentTypes.FirstOrDefault(a => a.TaxPaymentStatus == TaxPaymentStatus.AlreadyPaid).Id;

                _db.SaveChanges();

                return Json(new BasicDto() { info = "Uspješno sačuvana externa uplata.", error = "", id = pay.Id });
                

            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja uplate.", info = "" });
            }
        }
    }
}