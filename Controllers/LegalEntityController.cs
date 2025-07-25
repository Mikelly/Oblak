﻿using Kendo.Mvc.Extensions;
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
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Oblak.Data.Enums;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using Microsoft.AspNetCore.Mvc.Rendering;
using Oblak.Models.Payment;


namespace Oblak.Controllers
{
    public class LegalEntityController : Controller
    {
        ApplicationDbContext _db;
        HttpContext _context;
        ILogger<LegalEntityController> _logger;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;

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

            var username = _context?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
            }
        }

        [HttpGet]
        [Route("register-client", Name = "register")]
        public IActionResult Register()
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

            if (appUser.PartnerId == 3 || appUser.PartnerId == 4)
            {
                ViewBag.IsAdministered = true;
                ViewBag.IsPassThrough = true;
                ViewBag.Opstina = appUser.PartnerId == 3 ? "3" : "6";
            }
            else
            {
                ViewBag.IsAdministered = false;
                ViewBag.IsPassThrough = false;
                ViewBag.Opstina = "";
            }

            ViewBag.Opstine = new SelectList(_db.Municipalities.ToList(), "Id", "Name");

            return PartialView();
        }

        [HttpPost]
        [Route("register-client", Name = "registerClient")]
        public IActionResult RegisterClient(string name, string type, string address, string tin, bool isInVat, bool isAdministered, bool isPassThrough,
            string propertyName, string propertyExternalId, string propertyAddress, string propertyType, string propertyMunicipality, string propertyPlace,
            string phoneNumber, string email, string documentNumber, string regNumber, string regDate)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var legalEntity = appUser.LegalEntity;
            var country = legalEntity.Country;

            var errors = string.Empty;
            if (string.IsNullOrEmpty(name)) errors += "Morate unijeti naziv izdavaoca!" + Environment.NewLine;
            if (string.IsNullOrEmpty(type)) errors += "Morate unijeti tip izdavaoca!" + Environment.NewLine;
            if (string.IsNullOrEmpty(tin)) errors += "Morate unijeti JMBG izdavaoca!" + Environment.NewLine;
            if ((tin ?? "").Length != 13) errors += "Uneseni JMBG nema 13 karaktera!" + Environment.NewLine;
            if (string.IsNullOrEmpty(address)) errors += "Morate unijeti adresu izdavaoca!" + Environment.NewLine; ;
            if (!string.IsNullOrEmpty(regNumber) != !string.IsNullOrEmpty(regDate))
            {
                if (string.IsNullOrEmpty(regNumber))
                    errors += "Morate unijeti broj rješenja!" + Environment.NewLine;
                else
                    errors += "Morate unijeti datum isticanja rješenja!" + Environment.NewLine;
            }

            if (errors != string.Empty) return Json(new BasicDto() { error = errors, info = "" });

            var newLegalEntity = new LegalEntity();
            newLegalEntity.Name = name;
            newLegalEntity.Address = address;
            newLegalEntity.Country = country;
            newLegalEntity.Type = type == "Person" ? Data.Enums.LegalEntityType.Person : Data.Enums.LegalEntityType.Company;
            newLegalEntity.InVat = isInVat;
            newLegalEntity.TIN = tin;
            newLegalEntity.PartnerId = appUser.PartnerId;
            newLegalEntity.PhoneNumber = phoneNumber;
            newLegalEntity.Email = email;
            newLegalEntity.DocumentNumber = documentNumber;
            if (isAdministered) newLegalEntity.AdministratorId = legalEntity.Id;
            if (isPassThrough) newLegalEntity.PassThroughId = legalEntity.Id;

            var prop = (Property)null;

            if (string.IsNullOrEmpty(propertyName) == false)
            {
                prop = new Property();

                if (string.IsNullOrEmpty(propertyAddress)) errors += "Morate unijeti adresu objekta!" + Environment.NewLine;
                if (isPassThrough == false)
                {
                    if (string.IsNullOrEmpty(propertyExternalId)) errors += "Morate unijeti eksternu šifru objekta!" + Environment.NewLine;
                    prop.ExternalId = int.Parse(propertyExternalId);
                }
                //if (string.IsNullOrEmpty(propertyType)) errors += "Morate unijeti vrstu objekta!" + Environment.NewLine;

                if (isPassThrough == true)
                {
                    if (string.IsNullOrEmpty(propertyMunicipality)) errors += "Morate unijeti opštinu objekta!" + Environment.NewLine;
                    if (string.IsNullOrEmpty(propertyPlace)) errors += "Morate unijeti mjesto objekta!" + Environment.NewLine;
                }

                if (string.IsNullOrEmpty(propertyMunicipality) == false)
                {
                    var municipality = _db.Municipalities.Where(a => a.Id.ToString() == propertyMunicipality).FirstOrDefault();
                    prop.MunicipalityId = municipality.Id;
                }

                if (string.IsNullOrEmpty(propertyPlace) == false)
                {
                    prop.Place = propertyPlace;
                }

                prop.PropertyName = propertyName;
                prop.Name = propertyName;
                prop.Address = propertyAddress;
                prop.Type = propertyType;
                prop.RegNumber = regNumber;

                if (regDate != null)
                {
                    DateTime.TryParseExact(regDate, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date);
                    prop.RegDate = date;
                }
            }

            if (errors != string.Empty) return Json(new BasicDto() { error = errors, info = "" });

            try
            {
                _db.LegalEntities.Add(newLegalEntity);
                _db.SaveChanges();

                if (prop != null)
                {
                    prop.LegalEntityId = newLegalEntity.Id;
                    _db.Properties.Add(prop);
                    _db.SaveChanges(true);
                }
            }
            catch (Exception ex)
            {
                if (errors != string.Empty) errors += ex.Message + Environment.NewLine;
                return Json(new BasicDto() { error = errors, info = "" });
            }

            return Json(new BasicDto() { error = "", info = "Sve ok" });
        }


        [HttpPost]
        [Route("check-tin", Name = "checkTIN")]
        public IActionResult CheckTIN(string tin)
        {
            if (_appUser.PartnerId == 4)
            {
                bool exists = _db.LegalEntities.Any(le => le.TIN == tin);
                return Json(exists);
            }
            return Json(false);
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
            ViewBag.PartnerId = partner;

            return View();
        }

        [HttpGet("legalentity-select")]
        public IActionResult Select(int? legalEntity)
        {
            if (legalEntity != null)
            {
                var le = _db.LegalEntities.Where(a => a.Id == legalEntity).FirstOrDefault();

                ViewBag.Value = le.Id.ToString();
                ViewBag.Text = le.Name;
            }
            else
            {
                ViewBag.Id = null;
                ViewBag.Text = null;
            }

            return PartialView();
        }



        [HttpGet("legal-entity-read-admin")]
        public async Task<IActionResult> ReadAdmin(string text)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

            var data = await _db.LegalEntities.Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();

            text = (text ?? "").ToLower();

            var final = data
                .Where(a => a.Name.ToLower().Contains(text) || a.Address.ToLower().Contains(text))
                .Take(100)
                .ToList()
                .Select(_mapper.Map<LegalEntityDto>)
                .ToList();

            return Json(final);
        }

        [HttpPost("legal-entity-read")]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);

            var data = await _db.LegalEntities.Include(a => a.Properties).Where(x => x.PartnerId == appUser.PartnerId).OrderByDescending(x => x.Id).ToListAsync();

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
                var existingEntity = _db.LegalEntities.Include(a => a.Properties).FirstOrDefault(a => a.Id == input.Id);

                if (existingEntity == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                if (_db.MnePersons.Any(a => a.Property.LegalEntityId == input.Id) || _db.MnePersons.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje prijave vezane za njega." });

                if (_db.Groups.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje grupne prijave vezane za njega." });

                if (_db.ResTaxCalc.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje obračuni boravišne takse vezani za njega." });

                if (_db.Documents.Any(a => a.Property.LegalEntityId == input.Id))
                    return Json(new DataSourceResult { Errors = "Ne možete obrisati klijenta, jer postoje računi vezani za njega." });

                _db.RemoveRange(existingEntity.Properties);

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

            return Json(new BasicDto() { info = "SSL Certifikat uspješno obrisan!", error = "" });
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
         
        [HttpGet("legal-entity-is-registered")]
        public ActionResult IsRegistered(int legalEntityId)
        {
            if (_appUser.PartnerId == 4)
            {
                //najnovije resenje
                var prop = _db.Properties
                .Where(p => p.LegalEntityId == legalEntityId && p.RegDate != null)
                .OrderByDescending(p => p.RegDate)
                .FirstOrDefault();

                if (prop == null)
                {
                    return Json(new
                    {
                        isRegistered = false,
                        errInfo = "Nije moguća uplata jer korisnik nije registrovan!"
                    });
                }

                var date = prop.RegDate.Value;
                var diff = date - DateTime.Now.Date;

                // ako je resenje isteklo
                if (diff.TotalDays <= 0)
                {
                    return Json(new
                    {
                        isRegistered = false,
                        errInfo = $"Rješenje je isteklo! Datum isteka: <b>{date:dd.MM.yyyy}</b>"
                    });
                }
            }
             
            return Json(new { isRegistered = true });
        }


        [HttpGet("legal-entity-is-suspended")]
        public ActionResult IsSuspended(int? legalEntityId)
        {
            if (_appUser.PartnerId == 4)
            {
                bool le = _db.LegalEntities.Any(x => x.Id == legalEntityId && x.IsSuspended);
                string errInfo = string.Empty;
                if (le)
                {
                    return Json(new { isSuspended = true, errInfo = "Ovaj stanodavac je <b style=\"color: red;\">suspendovan</b> od strane turističke inspekcije!" });
                }
            }

            return Json(new { isSuspended = false });
        }

        [HttpGet("legal-entities-settings")]
        public IActionResult Settings(int legalEntityId)
        {
            var username = _context.User.Identity.Name;
            var appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == username);
            var legalEntity = appUser.LegalEntity;
            var partner = legalEntity.PartnerId;

            var ids = _db.Users.Join(_db.UserRoles, a => a.Id, b => b.UserId, (a, b) => new { a.LegalEntityId, b.RoleId }).Join(_db.Roles.Where(a => a.NormalizedName == "PROPERTYADMIN"), a => a.RoleId, b => b.Id, (a, b) => a.LegalEntityId).ToList();
            var admins = _db.LegalEntities.Where(a => a.PartnerId == partner).Where(a => ids.Contains(a.Id)).ToList();

            ViewBag.Admins = admins;
            ViewBag.PartnerId = partner;

            return View();
        }

        [HttpGet("legal-entity-payten-settings")]
        public async Task<ActionResult> PaytenSettingsAsync(int legalentity)
        {
            var existingEntity = await _db.LegalEntities.FindAsync(legalentity);

            if (existingEntity == null)
            { 
                return NotFound("Entity not found.");
            }

            var model = new PaytenSettingsViewModel
            {
                Name = existingEntity.Name,
                LegalEntityId = existingEntity.Id,
                PaytenUserId = existingEntity.PaytenUserId
            };

            return PartialView("_PaytenSettings", model);
        }

        [HttpPost("save-legal-entity-payten-settings")]
        public async Task<IActionResult> SavePaytenSettingsAsync(PaytenSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { error = "Podaci nisu validni." });
            }

            try
            { 
                var entity = await _db.LegalEntities.FindAsync(model.LegalEntityId);
                if (entity == null)
                {
                    return Json(new { error = "Entitet nije pronađen." });
                }
                 
                entity.PaytenUserId = string.IsNullOrEmpty(model.PaytenUserId) ? null : model.PaytenUserId; 

                await _db.SaveChangesAsync(); 

                return Json(new { id = entity.Id, info = "Uspješno ažurirano." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska prilikom azuriranja Payten podesavanja za LegalEntityId: {LegalEntityId}", model.LegalEntityId);
                return Json(new { error = ex.Message });
            }
        }

    }
}
