using AutoMapper;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
    public class ResTaxFeeController : Controller
    {
        private readonly ILogger<ResTaxFeeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;
        private HttpContext _context;

        public ResTaxFeeController(
            ILogger<ResTaxFeeController> logger, 
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

		[HttpGet("restax-fee")]
		public async Task<IActionResult> Index()
		{
            var data = _db.ResTaxPaymentTypes       
                           .Select(e => new SelectListItem
                           {
                               Value = e.Id.ToString(),
                               Text = e.Description
                           }).ToList();

            ViewBag.PaymentTypes = new SelectList(data, "Value", "Text");

            return View();
		}


		[HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            var username = _context!.User!.Identity!.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var data = await _db.ResTaxFees.Where(x => x.PartnerId == appUser.PartnerId).ToListAsync();            
            var resTaxFee = _mapper.Map<List<ResTaxFeeDto>>(data);
            return Json(await resTaxFee.ToDataSourceResultAsync(request));
        }


        [HttpPost]
        public async Task<ActionResult> Update(ResTaxFeeDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var tax = await _db.ResTaxFees.FindAsync(dto.Id);

                if (tax == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(dto, tax);

                await _db.SaveChangesAsync();

				var username = _context!.User!.Identity!.Name;
				var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
				var data = await _db.ResTaxFees.Where(x => x.PartnerId == appUser.PartnerId).ToListAsync();
				var resTaxFee = _mapper.Map<List<ResTaxFeeDto>>(data);
				return Json(await resTaxFee.ToDataSourceResultAsync(request));
			}
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(ResTaxFeeDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var username = _context!.User!.Identity!.Name;
                var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

                var tax = new ResTaxFee();
                _mapper.Map(dto, tax);
                tax.PartnerId = appUser!.PartnerId ?? 0;                
                _db.Add(tax);
                await _db.SaveChangesAsync();

				var data = await _db.ResTaxFees.Where(x => x.PartnerId == appUser.PartnerId).ToListAsync();
				var resTaxFee = _mapper.Map<List<ResTaxFeeDto>>(data);
				return Json(await resTaxFee.ToDataSourceResultAsync(request));
			}
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, ResTaxFeeDto dto)
        {
            try
            {
                var tax = _db.ResTaxFees.Find(dto.Id);                    

                if (tax != null)
                {                    
                    _db.ResTaxFees.Remove(tax);
                    _db.SaveChanges();

					var username = _context!.User!.Identity!.Name;
					var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
					var data = await _db.ResTaxFees.Where(x => x.PartnerId == appUser.PartnerId).ToListAsync();
					var resTaxFee = _mapper.Map<List<ResTaxFeeDto>>(data);
					return Json(await resTaxFee.ToDataSourceResultAsync(request));
				}
                else
                {
                    return Json(new { error = "Podešavanj nautičke takse nije pronađeno." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja podešavanja nautičke takse." });
            }
        }
    }
}