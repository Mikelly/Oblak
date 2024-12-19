using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Oblak.Data;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Oblak.Models;
using EllipticCurve.Utils;
using Oblak.Models.Api;
using System.IO.Compression;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using System.IO;
using SkiaSharp;
using Oblak.Data.Api;


namespace Oblak.Controllers
{
    public class AgencyController : Controller
    {
        ApplicationDbContext _db;
        HttpContext _context;
        ILogger<AgencyController> _logger;
        private readonly IMapper _mapper;

        public AgencyController(
            ApplicationDbContext db, 
            ILogger<AgencyController> logger,
            IHttpContextAccessor httpAccessor,
            IMapper mapper)
        {
            _db = db;
            _logger = logger;
            _mapper = mapper;
            _context = httpAccessor.HttpContext;
        }

        [HttpGet("agencies")]
        public IActionResult Index()
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var legalEntity = appUser.LegalEntity;
            var partner = legalEntity.PartnerId;

            var ids = _db.Users.Join(_db.UserRoles, a => a.Id, b => b.UserId, (a, b) => new { a.LegalEntityId, b.RoleId }).Join(_db.Roles.Where(a => a.NormalizedName == "PROPERTYADMIN"), a => a.RoleId, b => b.Id, (a, b) => a.LegalEntityId).ToList();
            var admins = _db.LegalEntities.Where(a => a.PartnerId == partner).Where(a => ids.Contains(a.Id)).ToList();

            ViewBag.Admins = admins;

            return View();
        }

        [HttpGet("agency-read-admin")]
        public async Task<IActionResult> ReadAdmin(string text)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

            var data = await _db.Agencies.Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();

            text = (text ?? "").ToLower();

            var final = data
                .Where(a => a.Name.ToLower().Contains(text) || a.Address.ToLower().Contains(text))
                .Take(100)
                .ToList()
                .Select(_mapper.Map<AgencyDto>)
                .ToList();

            return Json(final);
        }

        [HttpPost("agency-read")]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var data = await _db.Agencies.Include(a => a.Country).Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();
            var agencies = _mapper.Map<List<AgencyDto>>(data);
            return Json(await agencies.ToDataSourceResultAsync(request));
        }

        [HttpPost("agency-create")]
        public async Task<ActionResult> Create(AgencyDto input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var username = _context.User.Identity.Name;
                var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

                var agency = _mapper.Map<AgencyDto, Agency>(input);
                agency.PartnerId = appUser.PartnerId ?? 0;

                await _db.Agencies.AddAsync(agency);
                await _db.SaveChangesAsync();

                _db.Entry(agency).Reference(a => a.Country).Load();

                return Json(new[] { _mapper.Map(agency, input) }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost("agency-update")]
        public async Task<ActionResult> Update(AgencyDto input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var agency = await _db.Agencies.FindAsync(input.Id);

                if (agency == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(input, agency);

                await _db.SaveChangesAsync();

                return Json(new[] { _mapper.Map(agency, input) }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }


        [HttpPost("agency-destroy")]
        public async Task<ActionResult> Delete(AgencyDto input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var existingEntity = _db.Agencies.FirstOrDefault(a => a.Id == input.Id);

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                if(_db.ExcursionInvoices.Any(a => a.AgencyId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati agenciju, jer postoje fakture vezane za nju." });

                if (_db.Excursions.Any(a => a.AgencyId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati agenciju, jer postoje izleti za nju." });

                _db.Remove(existingEntity);

                await _db.SaveChangesAsync();

                return Json(new[] { _mapper.Map(existingEntity, input) }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }
    }
}
