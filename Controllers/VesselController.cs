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
using Microsoft.AspNetCore.Mvc.Rendering;
using Oblak.Data.Enums;


namespace Oblak.Controllers
{
    public class VesselController : Controller
    {
        ApplicationDbContext _db;
        HttpContext _context;
        ILogger<VesselController> _logger;
        private readonly IMapper _mapper;

        public VesselController(
            ApplicationDbContext db, 
            ILogger<VesselController> logger,
            IHttpContextAccessor httpAccessor,
            IMapper mapper)
        {
            _db = db;
            _logger = logger;
            _mapper = mapper;
            _context = httpAccessor.HttpContext;
        }
        

        [HttpGet("vessels")]
        public IActionResult Index()
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var legalEntity = appUser.LegalEntity;
            var partner = legalEntity.PartnerId;

            var ids = _db.Users.Join(_db.UserRoles, a => a.Id, b => b.UserId, (a, b) => new { a.LegalEntityId, b.RoleId }).Join(_db.Roles.Where(a => a.NormalizedName == "PROPERTYADMIN"), a => a.RoleId, b => b.Id, (a, b) => a.LegalEntityId).ToList();
            var admins = _db.LegalEntities.Where(a => a.PartnerId == partner).Where(a => ids.Contains(a.Id)).ToList();

            ViewBag.Admins = admins;

            var data = Enum.GetValues(typeof(VesselType))
               .Cast<VesselType>()
               .Select(e => new SelectListItem
               {
                   Value = e.ToString(),
                   Text = Helpers.EnumExtensions.GetEnumDisplayName(e)
               })
               .ToList();

            ViewBag.VesselTypes = new SelectList(data, "Value", "Text");

            return View();
        }

        [HttpGet("vessel-read-admin")]
        public async Task<IActionResult> ReadAdmin(string text)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var data = await _db.Vessels.Include(a => a.LegalEntity).Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();

            text = (text ?? "").ToLower();

            List<VesselDto> final = data
                .Where(a => a.Name.ToLower().Contains(text) || a.OwnerName.ToLower().Contains(text) || a.Registration.ToLower().Contains(text))
                .ToList()
                .Select(_mapper.Map<VesselDto>)
                .ToList();

            return Json(final);
        }

        [HttpPost("vessel-read")]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.FirstOrDefault(a => a.UserName == username);
            var data = await _db.Vessels.Include(a => a.LegalEntity).Include(a => a.Country).Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();

            var vessels = _mapper.Map<List<VesselDto>>(data);

            return Json(await vessels.ToDataSourceResultAsync(request));
        }

        [HttpPost("vessel-create")]
        public async Task<ActionResult> Create(VesselDto input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var username = _context.User.Identity.Name;
                var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

                var vessel = _mapper.Map<Vessel>(input);
                vessel.PartnerId = appUser!.PartnerId ?? 0;

                //var validation = _registerClient.Validate(newGuest, newGuest.CheckIn, newGuest.CheckOut);

                //if (validation.ValidationErrors.Any())
                //{
                //    return Json(new { success = false, errors = validation.ValidationErrors });
                //}

                await _db.Vessels.AddAsync(vessel);
                await _db.SaveChangesAsync();

                var data = await _db.Vessels.Include(a => a.LegalEntity).Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();
                var vessels = _mapper.Map<List<VesselDto>>(data);
                return Json(await vessels.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost("vessel-update")]
        public async Task<ActionResult> Update(VesselDto input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var existingEntity = await _db.Vessels.FindAsync(input.Id);

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(input, existingEntity);

                // validation

                await _db.SaveChangesAsync();

                return Json(new[] { _mapper.Map(existingEntity, input) }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }


        [HttpPost("vessel-destroy")]
        public async Task<ActionResult> Delete(LegalEntityViewModel input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var existingEntity = _db.Vessels.FirstOrDefault(a => a.Id == input.Id);

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                if(_db.Groups.Any(a => a.VesselId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati plovilo, jer postoje prijave vezane za njega." });

                _db.Remove(existingEntity);

                await _db.SaveChangesAsync();

                var vesselDto = _mapper.Map<VesselDto>(existingEntity);

                return Json(new[] { vesselDto }.ToDataSourceResult(request, ModelState));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }


        [HttpPost("vessel-legalentity-select")]
        public ActionResult SelectLegalEntity(int vessel, int legalentity)
        {
            var v = _db.Vessels.FirstOrDefault(a => a.Id == vessel);
            v.LegalEntityId = legalentity;
            _db.SaveChanges();

            return Ok();
        }


        [HttpPost("create-from-owner")]
        public IActionResult CreateFromOwner(int vessel, string name, string address, string phone, string tin)
        {
            try
            {
                var username = _context.User.Identity.Name;
                var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
                var legalEntity = appUser.LegalEntity;
                var country = legalEntity.Country;

                var le = new LegalEntity();
                le.Name = name;
                le.Address = address;
                le.TIN = tin ?? "-";
                le.PhoneNumber = phone;
                le.Type = Data.Enums.LegalEntityType.Person;
                le.Country = country;
                le.AdministratorId = legalEntity.Id;
                le.PassThroughId = legalEntity.Id;
                le.PartnerId = appUser.PartnerId;

                _db.LegalEntities.Add(le);
                _db.SaveChanges();

                var v = _db.Vessels.FirstOrDefault(a => a.Id == vessel);
                v.LegalEntityId = le.Id;
                _db.SaveChanges();

                return Ok(new { le = le.Id, info = "Uspješno kreiran izdavalac!", error = "" });
            }
            catch (Exception ex)
            {
                return Ok(new { le = -1, info = "", error = ex.Message });
            }
        }

    }
}
