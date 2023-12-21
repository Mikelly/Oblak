using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Oblak.Data;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Oblak.Models.Api;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Oblak.Models;

namespace Oblak.Controllers
{
    public class LegalEntityController : Controller
    {
        ApplicationDbContext _db;
        ILogger<LegalEntityController> _logger;
        private readonly IMapper _mapper;

        public LegalEntityController(
            ApplicationDbContext db, 
            ILogger<LegalEntityController> logger,
            IMapper mapper)
        {
            _db = db;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet("clients")]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = await _db.LegalEntities.OrderByDescending(x => x.Id).ToListAsync();

            var legalEntities = _mapper.Map<List<LegalEntityViewModel>>(data);

            return Json(await legalEntities.ToDataSourceResultAsync(request));
        }

        [HttpPost]
        public async Task<ActionResult> Create(LegalEntityViewModel input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var legalEntity = _mapper.Map<LegalEntityViewModel, LegalEntity>(input);

                //var validation = _registerClient.Validate(newGuest, newGuest.CheckIn, newGuest.CheckOut);

                //if (validation.ValidationErrors.Any())
                //{
                //    return Json(new { success = false, errors = validation.ValidationErrors });
                //}

                await _db.LegalEntities.AddAsync(legalEntity);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost("clients")]
        public async Task<ActionResult> Update(LegalEntityViewModel input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var existingEntity = await _db.LegalEntities.FindAsync(input.Id);

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

        [HttpGet]
        [Route("srb-cred", Name = "SrbCred")]
        public IActionResult SrbCred(int legalEntity)
        {
            var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
            ViewBag.LegalEntity = legalEntity;
            ViewBag.Password = firma.SrbRbPassword;
            ViewBag.UserName = firma.SrbRbUserName;
            return PartialView();
        }

        [HttpPost]
        [Route("srb-cred", Name = "SrbCred")]
        public IActionResult SrbCred(int legalEntity, string username, string password)
        {
            var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
            firma.SrbRbPassword = password;
            firma.SrbRbUserName = username;
            _db.SaveChanges();
            return View();
        }

        [HttpGet]
		[Route("upload-cert", Name = "UploadCertGet")]
		public IActionResult UploadCert(string type, int legalEntity)
        {
            ViewBag.Type = type;
            ViewBag.LegalEntity = legalEntity;
            return View();
        }

        [HttpPost]
        [Route("upload-cert", Name = "UploadCert")]
        public async Task<IActionResult> UploadCert([FromForm] string certType, [FromForm] int legalEntity, [FromForm] IFormFile certFile, [FromForm] string certPassword)
        {
            try
            {
                var bytes = await certFile.GetBytes();

                X509Certificate2 certificate = new X509Certificate2(bytes, certPassword, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

                var vatme_pl = certificate.Subject.Substring(certificate.Subject.IndexOf("VATME-") + 6, 8);
                var vatme_fl = certificate.Subject.Substring(certificate.Subject.IndexOf("VATME-") + 6, 13);

                var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
                if (firma != null)
                {
                    if (certType == "rb90")
                    {
                        firma.Rb90CertData = bytes;
                        firma.Rb90Password = certPassword;                        
                    }
                    if (certType == "efi")
                    {
                        firma.EfiCertData = bytes;
                        firma.EfiPassword = certPassword;
                    }
                    _db.SaveChanges();
                }
                
                //certificate.Import(bytes, certPassword, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
                //var vat = certificate.Subject.Substring(certificate.Subject.IndexOf("VATME-") + 6, 8);
                //var vat1 = certificate.Subject.Substring(certificate.Subject.IndexOf("VATME-") + 6, 13);
                //if (vat != pib && vat1 != pib)
                //{
                //    ModelState.AddModelError("PIB", "PIB koji ste unijeli ne poklapa se sa PIB-om certifikata!");
                //    ModelState.AddModelError("SSLCERT", "PIB koji ste unijeli ne poklapa se sa PIB-om certifikata!");
                //}

            }
            catch (CryptographicException ex)
            {
                if ((ex.HResult & 0xFFFF) == 0x56)
                {
                    ModelState.AddModelError("SSLPASS", "Neispravna lozinka za sertifikat!");
                }
                else
                {
                    ModelState.AddModelError("SSLCERT", "Nijeste odabrali odgovarajući fajl, ili je sertifikat neispravan!");
                }
            }

            return View();
        }        
    }
}
