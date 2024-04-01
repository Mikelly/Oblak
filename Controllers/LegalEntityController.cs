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

namespace Oblak.Controllers
{
    public class LegalEntityController : Controller
    {
        ApplicationDbContext _db;
        HttpContext _context;
        ILogger<LegalEntityController> _logger;
        private readonly IMapper _mapper;

        public LegalEntityController(
            ApplicationDbContext db, 
            ILogger<LegalEntityController> logger,
            IHttpContextAccessor httpAccessor,
            IMapper mapper)
        {
            _db = db;
            _logger = logger;
            _mapper = mapper;
            _context = httpAccessor.HttpContext;
        }

        [HttpGet]
        [Route("register-client", Name = "register")]
        public IActionResult Register()
        {
            return PartialView();
        }

        [HttpPost]
        [Route("register-client", Name = "registerClient")]
        public IActionResult RegisterClient(string name, string type, string address, string tin, bool isInVat, bool isAdministered, bool isPassThrough, 
            string propertyName, string propertyExternalId, string propertyAddress, string propertyType)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var legalEntity = appUser.LegalEntity;
            var country = legalEntity.Country;

            var errors = string.Empty;
            if (string.IsNullOrEmpty(name)) errors += "Morate unijeti naziv izdavaoca!" + Environment.NewLine;
            if (string.IsNullOrEmpty(type)) errors += "Morate unijeti tip izdavaoca!" + Environment.NewLine;
            if (string.IsNullOrEmpty(tin)) errors += "Morate unijeti poreski broj izdavaoca!" + Environment.NewLine;
            if (string.IsNullOrEmpty(address)) errors += "Morate unijeti adresu izdavaoca!" + Environment.NewLine;

            if (errors != string.Empty) return Json(new BasicDto() { error = errors, info = "" });

            var newLegalEntity = new LegalEntity();
            newLegalEntity.Name = name;
            newLegalEntity.Address = address;
            newLegalEntity.Country = country;
            newLegalEntity.Type = type == "Person" ? Data.Enums.LegalEntityType.Person : Data.Enums.LegalEntityType.Company;
            newLegalEntity.InVat = isInVat;
            newLegalEntity.TIN = tin;
            newLegalEntity.PartnerId = appUser.PartnerId;
            if (isAdministered) newLegalEntity.AdministratorId = legalEntity.Id;
            if (isPassThrough) newLegalEntity.PassThroughId = legalEntity.Id;

            if (string.IsNullOrEmpty(propertyName) == false)
            {
                if (string.IsNullOrEmpty(propertyAddress)) errors += "Morate unijeti adresu objekta!" + Environment.NewLine;
                if(isPassThrough == false) if (string.IsNullOrEmpty(propertyExternalId)) errors += "Morate unijeti eksternu šifru objekta!" + Environment.NewLine;
                //if (string.IsNullOrEmpty(propertyType)) errors += "Morate unijeti vrstu objekta!" + Environment.NewLine;
            }

            if (errors != string.Empty) return Json(new BasicDto() { error = errors, info = "" });

            try
            {
                _db.LegalEntities.Add(newLegalEntity);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                if (errors != string.Empty) errors += ex.Message + Environment.NewLine;
                return Json(new BasicDto() { error = errors, info = "" });
            }

            return Json(new BasicDto() { error = "", info = "Sve ok" });
        }

        [HttpGet("clients")]
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

        [HttpPost("legal-entity-read")]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

            var data = await _db.LegalEntities.Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();

            var legalEntities = _mapper.Map<List<LegalEntityViewModel>>(data);

            return Json(await legalEntities.ToDataSourceResultAsync(request));
        }

        [HttpPost("legal-entity-create")]
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

        [HttpPost("legal-entity-update")]
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


        [HttpPost("legal-entity-destroy")]
        public async Task<ActionResult> Delete(LegalEntityViewModel input, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var existingEntity = await _db.LegalEntities.FindAsync(input.Id);

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                if(_db.MnePersons.Any(a => a.Property.LegalEntityId == input.Id) || _db.MnePersons.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje prijave vezane za njega." });

                if (_db.Groups.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje grupne prijave vezane za njega." });

                if (_db.ResTaxCalc.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje obračuni boravišne takse vezani za njega." });

                if (_db.Documents.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje računi vezani za njega." });

                _db.Remove(existingEntity);

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
            return PartialView();
        }

        [HttpGet]
		[Route("upload-cert", Name = "UploadCertGet")]
		public IActionResult UploadCert(string certType, int legalEntity)
        {
			var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);

            if (certType == "rb90")
            {
                var certData = firma.Rb90CertData;
				var certPassword = firma.Rb90Password;
                if (certData == null)
                {
                    ViewBag.CERT = false;
                }
                else
                {
                    X509Certificate2 certificate = new X509Certificate2(certData, certPassword, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
                    ViewBag.CERT = true;
                    ViewBag.ValidFrom = certificate.NotBefore;
					ViewBag.ValidTo = certificate.NotAfter;
				}
			}
            if (certType == "efi")
            {
                var certData = firma.EfiCertData;
                var certPassword = firma.EfiPassword;
                if (certData == null)
                {
                    ViewBag.CERT = false;
                }
                else
                {
                    X509Certificate2 certificate = new X509Certificate2(certData, certPassword, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
                    ViewBag.CERT = true;
                    ViewBag.ValidFrom = certificate.NotBefore;
                    ViewBag.ValidTo = certificate.NotAfter;
                }
            }


			ViewBag.CertType = certType;
            ViewBag.LegalEntity = legalEntity;
            return PartialView();
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
            }
            catch (CryptographicException ex)
            {
                if ((ex.HResult & 0xFFFF) == 0x56)
                {
					return Json(new BasicDto() { info = "", error = "Neispravna lozinka za sertifikat!" });
					
                }
                else
                {
					return Json(new BasicDto() { info = "", error = "Nijeste odabrali odgovarajući fajl, ili je sertifikat neispravan!" });					
                }
            }

            return Json(new BasicDto() { info = "SSL uspješno sačuvan!", error = "" });
        }

        [HttpGet]
        [Route("download-cert", Name = "DownloadCert")]
        public async Task<FileResult> DownloadCert(string certType, int legalEntity)
        {
            try
            {
                var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
                
                byte[] bytes = new byte[0];
                string certPassword = string.Empty;

                if (certType == "rb90")
                {
                    bytes = firma.Rb90CertData;
                    certPassword = firma.Rb90Password;
                }
                if (certType == "efi")
                {
                    bytes = firma.EfiCertData;
                    certPassword = firma.EfiPassword;                        
                }

                var invalids = System.IO.Path.GetInvalidFileNameChars();
                var newName = string.Join("_", firma.Name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
                                
                using (MemoryStream zipStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        var entry1 = archive.CreateEntry($"{newName}.txt", CompressionLevel.Fastest);
                        using (Stream stream = entry1.Open())
                        {
                            byte[] passwordBytes = Encoding.Unicode.GetBytes(certPassword);
                            await stream.WriteAsync(passwordBytes, 0, passwordBytes.Length);
                        }
                        var entry2 = archive.CreateEntry($"{newName}.pfx", CompressionLevel.Fastest);
                        using (Stream stream = entry2.Open())
                        {
                            byte[] passwordBytes = Encoding.Unicode.GetBytes(certPassword);                            
                            await stream.WriteAsync(bytes, 0, bytes.Length);
                        }
                    }// disposal of archive will force data to be written to memory stream.
                    zipStream.Position = 0; //reset memory stream position.
                    var finalbytes = zipStream.ToArray(); //get all flushed data
                    return File(finalbytes, "application/zip", $"{newName}_{certType}.zip");                     
                }
            }
            catch (CryptographicException ex)
            {
                return null;
            }
        }

        [HttpPost]
		[Route("delete-cert", Name = "DeleteCert")]
		public async Task<IActionResult> DeleteCert([FromForm] string certType, [FromForm] int legalEntity)
		{
			var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
			if (firma != null)
			{
				if (certType == "rb90")
				{
					firma.Rb90CertData = null;
					firma.Rb90Password = null;
				}
				if (certType == "efi")
				{
					firma.EfiCertData = null;
					firma.EfiPassword = null;
				}
				_db.SaveChanges();
			}

			return Json(new BasicDto(){ info = "SSL Certifikat uspješno obrisan!", error = "" });
		}


        [HttpGet]
        [Route("upload-logo", Name = "UploadLogoGet")]
        public IActionResult UploadLogo(int legalEntity, string hide)
        {
            var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);            
            ViewBag.LegalEntity = legalEntity;
            ViewBag.Logo = firma.Logo;
            ViewBag.Hide = hide == "Y";
            return PartialView();
        }

        [HttpPost]
        [Route("upload-logo", Name = "UploadLogo")]
        public async Task<IActionResult> Uploadlogo([FromForm] int legalEntity, [FromForm] IFormFile logo)
        {
            try
            {
                var bytes = await logo.GetBytes();

                var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
                if (firma != null)
                {
                    firma.Logo = bytes;
                    _db.SaveChanges();
                }
            }
            catch (CryptographicException ex)
            {
                return Json(new BasicDto() { info = "", error = "Nijeste odabrali odgovarajući fajl, ili je sertifikat neispravan!" });
            }

            return Json(new BasicDto() { info = "Logo uspješno sačuvan!", error = "" });
        }


        [HttpGet]
        [Route("upload-header", Name = "UploadHeaderGet")]
        public IActionResult UploadHeader(int legalEntity)
        {
            var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
            ViewBag.Header = firma.InvoiceHeader;
            ViewBag.LegalEntity = legalEntity;
            return PartialView();
        }


        [HttpPost]
        [Route("upload-header", Name = "UploadHeader")]
        public async Task<IActionResult> UploadHeader([FromForm] int legalEntity, [FromForm] string header)
        {
            try
            {
                var firma = _db.LegalEntities.FirstOrDefault(a => a.Id == legalEntity);
                if (firma != null)
                {
                    firma.InvoiceHeader = header;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return Json(new BasicDto() { info = "", error = ex.Message });
            }

            return Json(new BasicDto() { info = "Zaglavlje računa uspješno sačuvano!", error = "" });
        }
    }
}
