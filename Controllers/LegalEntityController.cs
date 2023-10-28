using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Oblak.Data;
using Oblak.Models;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Oblak.Controllers
{
    public class LegalEntityController : Controller
    {
        ApplicationDbContext _db;
        ILogger<LegalEntityController> _logger;

        public LegalEntityController(
            ApplicationDbContext db, 
            ILogger<LegalEntityController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("clients")]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = _db.LegalEntities.ToList();
            return Json(data.ToDataSourceResult(request));
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
