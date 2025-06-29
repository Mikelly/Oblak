using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Helpers;
using Oblak.Migrations;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;
using System.Xml.Linq;
using Telerik.Reporting;
using Telerik.SvgIcons;

namespace Oblak.Controllers
{
    public class ReportController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReportController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly ApplicationUser _appUser;
        private readonly Register _registerClient;
        private LegalEntity _legalEntity;

        public ReportController(
            IWebHostEnvironment env,
            ILogger<ReportController> logger,
            ApplicationDbContext db,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _env = env;
            _logger = logger;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
                _legalEntity = _appUser.LegalEntity;
                if (_appUser.LegalEntity.Country == CountryEnum.MNE) _registerClient = serviceProvider.GetRequiredService<MneClient>();
                if (_appUser.LegalEntity.Country == CountryEnum.SRB) _registerClient = serviceProvider.GetRequiredService<SrbClient>();
            }
        }

        [HttpPost]
        [Route("upload-report")]
        public async Task<IActionResult> Upload(IFormFile reportFile)
        {
            var contentPath = _env.ContentRootPath;
            var path = Path.Combine(contentPath, "Reports", reportFile.FileName);

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                await reportFile.CopyToAsync(fileStream);
            }

            var report = new Oblak.Data.Report();
            report.Name = reportFile.Name;
            report.Path = reportFile.FileName;
            _db.Reports.Add(report);
            _db.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Route("run-report")]
        public FileResult Run(int id)
        {
            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var report = _db.Reports.FirstOrDefault(a => a.Id == id);
            var path = Path.Combine(_env.ContentRootPath, "Reports", report.Path);

            reportSource.Uri = path;

            object parameterValue = "mil";
            reportSource.Parameters.Add("ime", parameterValue);

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

            if (!result.HasErrors)
            {
                return File(result.DocumentBytes, "application/pdf", "report");
            }

            return null;
        }

        public IActionResult Index()
        {
            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

            // set any deviceInfo settings if necessary
            var deviceInfo = new System.Collections.Hashtable();

            var reportSource = new Telerik.Reporting.UriReportSource();

            //// reportName is the path to the TRDP/TRDX file
            reportSource.Uri = @"C:\\Reports\Report1.trdp";

            object parameterValue = "mil";
            reportSource.Parameters.Add("ime", parameterValue);

            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

            if (!result.HasErrors)
            {

                return File(result.DocumentBytes, "application/pdf");
                //string fileName = result.DocumentName + "." + result.Extension;
                //string path = System.IO.Path.GetTempPath();
                //string filePath = System.IO.Path.Combine(path, fileName);

                //using (System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                //{
                //    fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
                //}
            }

            return Ok();
        }

        private List<(string, string, object)> GetParameters(string reportPath)
        {
            var reportPackager = new ReportPackager();
            using (var sourceStream = System.IO.File.OpenRead(reportPath))
            {
                var report = (Telerik.Reporting.Processing.Report)reportPackager.UnpackageDocument(sourceStream);

                return report.Parameters.Select(a => a.Value).Select(a => (a.Name, a.Type, a.Value)).ToList();
            }
        }

        private void ApplyParameters(UriReportSource uriReportSource)
        {
            foreach ((string name, string type, object value) in GetParameters(uriReportSource.Uri))
            {
                var formValue = "";
                object v = type switch
                {
                    "String" => formValue.ToString(),
                    "Integer" => formValue != null ? int.Parse(formValue.ToString()) : value,
                    _ => ""
                };
            }
        }

        [HttpGet]
        [Route("reports-res-tax")]
        [Authorize(Roles = "TouristOrgControllor,TouristOrgAdmin")]
        public ActionResult TouristOrgResTax()
        {
            return TouristOrg("R");
        }

        [HttpGet]
        [Route("reports-res-tax-o")]
        public ActionResult TouristOrgResTaxOperater()
        {
            return TouristOrg("O");
        }

        [HttpGet]
        [Route("reports-exc-tax")]
        [Authorize(Roles = "TouristOrgControllor,TouristOrgAdmin")]
        public ActionResult TouristOrgExcTax()
        {
            return TouristOrg("E");
        }

        [HttpGet]
        [Route("reports-mup")]
        [Authorize(Roles = "TouristOrgControllor,TouristOrgAdmin")]
        public ActionResult TouristOrgMup()
        {
            return TouristOrg("M");
        }

        [HttpGet]
        [Route("reports-nau-tax")]
        [Authorize(Roles = "TouristOrgControllor,TouristOrgAdmin")]
        public ActionResult TouristOrgNauTax()
        {
            return TouristOrg("N");
        }

        [HttpGet]
        [Route("reports-tourist-org")]
        public ActionResult TouristOrg(string taxType)
        {
            var admin = HttpContext.User.IsInRole("TouristOrgAdmin") || HttpContext.User.IsInRole("TouristOrgController");
            var oper = !admin;

            ViewBag.Admin = admin;
            ViewBag.UserType = admin ? "Admin" : "Operator";

            var checkpoints = _db.CheckInPoints.Where(a => a.PartnerId == _legalEntity.PartnerId).ToList();
            //ViewBag.CheckInPoints = new SelectList(checkpoints, "Id", "Name");
            ViewBag.CheckInPoints = checkpoints;

            var users = _db.Users.Where(a => a.PartnerId == _legalEntity.PartnerId).ToList();
            ViewBag.Users = new SelectList(users, "UserName", "PersonName");

            var taxPayTypes = _db.TaxPaymentTypes.Where(a => a.PartnerId == _legalEntity.PartnerId).ToList();
            ViewBag.TaxPayTypes = new SelectList(taxPayTypes, "Id", "Description");

            var mun = _appUser.PartnerId == 3 ? "20010" : "20036";

            var places = _db.CodeLists.Where(a => a.Country == "MNE" && a.Type == "mjesto" && a.Param1 == mun).ToList();

            ViewBag.Places = places;
            ViewBag.TaxType = taxType;
            ViewBag.PartnerId = _legalEntity.PartnerId.ToString();

            var legalEntity = _db.LegalEntities.Find(_appUser.LegalEntityId);
            FiscalEnu? fiscalEnu = new FiscalEnu();
            if (legalEntity != null)
            {
                using var _ = _registerClient.Initialize(_legalEntity);
                var properties = _registerClient.GetProperties();
                var fProp = properties.Result.FirstOrDefault();
                if (fProp != null)
                {
                    fiscalEnu = _db.FiscalEnu.FirstOrDefault(a => a.PropertyId == fProp.Id);
                }
            }
            ViewBag.ENUCode = fiscalEnu?.FiscalEnuCode;

            return View("TouristOrg");
        }


        [HttpPost]
        [Route("/treport")]
        public async Task<FileStreamResult> TouristOrgRunReport()
        {
            var report = Request.Form["Report"];
            var format = Request.Form["Format"];
            var legalEntityName = Request.Form["LegalEntityName"];
            var date = Request.Form["Date"];
            var dateFrom = Request.Form["DateFrom"];
            var dateTo = Request.Form["DateTo"];
            var checkInPoint = Request.Form["CheckInPointId"];
            var username = Request.Form["UserName"];
            var resTaxGroup = Request.Form["ResTaxGroup"];
            var place = Request.Form["Place"];
            var legalEntityStatus = Request.Form["LegalEntityStatus"];
            var legalEntity = Request.Form["LegalEntity"];
            var taxPaymentType = Request.Form["TaxPaymentType"];
            var firstName = Request.Form["FirstName"];
            var lastName = Request.Form["LastName"];
            var documentNumber = Request.Form["DocumentNumber"];
            var agency = Request.Form["AgencyId"];
            var enuCode = Request.Form["ENUCode"];

            //Dictionary<string, object> parameters = new Dictionary<string, object>();
            List<Parameter> parameters = new List<Parameter>();

            if (report == "CountryStats")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "Date", Value = DateTime.ParseExact(date, "ddMMyyyy", null) });
            }
            else if (report == "CountryMupAgeStatsPeriod")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "LegalEntityname", Value = string.IsNullOrEmpty(legalEntityName) ? "-1" : legalEntityName }); 
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "CountryStatsPeriod" || report == "ResTaxHistory" || report.ToString().StartsWith("ExcursionTax") || report.ToString().StartsWith("CountryMup"))
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "TaxExemptions")
            {
                int.TryParse(checkInPoint, out var cpid);
                var cp = _db.CheckInPoints.FirstOrDefault(a => a.Id == cpid);

                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "LegalEntity", Value = (string)legalEntity == "" ? "-1" : (string)legalEntity });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "CheckInPoint", Value = cpid == 0 ? -1 : cp.Id });
                parameters.Add(new Parameter() { Name = "UserName", Value = ((string)username == "") ? "-1" : username });
            }
            else if (report == "GuestBook" || report == "GuestBookPayments" || report == "Ledger")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "LegalEntity", Value = legalEntity });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "GuestList")
            {
                parameters.Add(new Parameter() { Name = "PartnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
                if (User.IsInRole("TouristOrgAdmin"))
                {
                    parameters.Add(new Parameter() { Name = "CheckInPointId", Value = int.Parse(checkInPoint) });
                    parameters.Add(new Parameter() { Name = "User", Value = username });
                }
                else
                {
                    parameters.Add(new Parameter() { Name = "CheckInPointId", Value = _appUser.CheckInPointId });
                    parameters.Add(new Parameter() { Name = "User", Value = User.Identity.Name });
                }
            }
            else if (report == "GuestListByPlace")
            {
                parameters.Add(new Parameter() { Name = "PartnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "ResTax" || report == "ResTaxTotal")
            {
                parameters.Add(new Parameter() { Name = "PartnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "Group", Value = resTaxGroup });
            }
            else if (report.ToString().StartsWith("ExcursionTax"))
            {
                parameters.Add(new Parameter() { Name = "PartnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "LegalEntities")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "Status", Value = legalEntityStatus });
                parameters.Add(new Parameter() { Name = "Place", Value = place });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "GuestHistory")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "FirstName", Value = firstName });
                parameters.Add(new Parameter() { Name = "LastName", Value = lastName });
                parameters.Add(new Parameter() { Name = "DocumentNumber", Value = documentNumber });
            }
            else if (report == "Debt")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
            }
            else if (report == "PostOffice")
            {
                var cpid = int.Parse(checkInPoint);
                var cp = _db.CheckInPoints.FirstOrDefault(a => a.Id == cpid);

                parameters.Add(new Parameter() { Name = "PartnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "Date", Value = DateTime.ParseExact(date, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "CheckInPoint", Value = cpid });
                parameters.Add(new Parameter() { Name = "CheckInPointName", Value = cp.Name });
                parameters.Add(new Parameter() { Name = "TaxType", Value = "ResidenceTax" });
                parameters.Add(new Parameter() { Name = "TaxTypeName", Value = "Boravišna taksa" });
                parameters.Add(new Parameter() { Name = "id", Value = 0 });
                parameters.Add(new Parameter() { Name = "g", Value = 0 });
                parameters.Add(new Parameter() { Name = "inv", Value = 0 });
                parameters.Add(new Parameter() { Name = "pay", Value = 0 });
            }
            else if (report == "ResTax")
            {
                parameters.Add(new Parameter() { Name = "Date", Value = DateTime.ParseExact(date, "ddMMyyyy", null) });
                if (User.IsInRole("TouristOrgAdmin"))
                {
                    parameters.Add(new Parameter() { Name = "CheckInPointId", Value = int.Parse(checkInPoint) });
                    parameters.Add(new Parameter() { Name = "User", Value = username });
                }
                else
                {
                    parameters.Add(new Parameter() { Name = "CheckInPointId", Value = _appUser.CheckInPointId });
                    parameters.Add(new Parameter() { Name = "Radnik", Value = User.Identity.Name });
                }
            }
            else if (report == "ExcursionCountryStats")
            {
                parameters.Add(new Parameter() { Name = "PartnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "Excursion")
            {
                parameters.Add(new Parameter() { Name = "Agencija", Value = int.Parse(agency) });
                parameters.Add(new Parameter() { Name = "od", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "do", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "PeriodicFiscal")
            {
                parameters.Add(new Parameter() { Name = "enu", Value = enuCode });
                parameters.Add(new Parameter() { Name = "od", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "do", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "NbrGuestPerPC")
            {
                parameters.Add(new Parameter() { Name = "partnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "dateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "dateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "NauticalTax")
            {
                parameters.Add(new Parameter() { Name = "partnerid", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "dateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "dateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else if (report == "CountryStatsFirmaPeriod")
            {
                parameters.Add(new Parameter() { Name = "firmaid", Value = _legalEntity.Id });
                parameters.Add(new Parameter() { Name = "datefrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "dateto", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }

                var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var toReport = $"{_legalEntity.PartnerId}{report}.trdp";
            var rep = _db.Reports.FirstOrDefault(a => a.Name == toReport);
            var path = Path.Combine(_env.ContentRootPath, "Reports", toReport);

            reportSource.Uri = path;

            try
            {
                reportSource.Parameters.AddRange(parameters);

                Response.Headers.Append("Cache-Control", "private, max-age=1800");
                Response.Headers.Append("Content-Disposition", "inline; filename=Report.pdf");

                Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport(format, reportSource, deviceInfo);

                if (result.HasErrors)
                {
                    _logger.LogError("Telerik report - greske za {Report}:", report);
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError(" - {Error}", error.Message);
                    }

                    byte[] errorPdf = new Pdf().GenerateErrorPdf($"Report '{report}' greska pri renderovanju.");
                     
                    var errorStream = new MemoryStream(errorPdf);
                    errorStream.Seek(0, SeekOrigin.Begin);
                    return new FileStreamResult(errorStream, "application/pdf")
                    {
                        FileDownloadName = "ReportError.pdf"
                    };
                }
                
                var stream = new MemoryStream(result.DocumentBytes);
                stream.Seek(0, SeekOrigin.Begin);
                var fsr = new FileStreamResult(stream, "application/pdf");
                fsr.FileDownloadName = $"Report.pdf";

                return fsr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception - Telerik report {Report}. Inner: {InnerException}", report, ex.InnerException?.Message);

                byte[] errorPdf = new Pdf().GenerateErrorPdf($"Report '{report}' exception tokom generisanja.\n{ex.Message}");
                 
                var errorStream = new MemoryStream(errorPdf);
                errorStream.Seek(0, SeekOrigin.Begin);
                return new FileStreamResult(errorStream, "application/pdf")
                {
                    FileDownloadName = "ReportException.pdf"
                };
            }
        }

        [HttpGet]
        [Route("/generate-mne-treport")]
        public async Task<FileStreamResult> PeriodicFiscal([FromQuery] string report, [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] string enuCode)
        {
            string mnePartnerId = "2";
            List<Parameter> parameters = new List<Parameter>();

            if (string.IsNullOrEmpty(report) || string.IsNullOrEmpty(dateFrom) || string.IsNullOrEmpty(dateTo) || string.IsNullOrEmpty(enuCode))
            { 
                byte[] errorPdf = new Pdf().GenerateErrorPdf("Nedostaju neophodni parametri.");
                var errorStream = new MemoryStream(errorPdf);
                errorStream.Seek(0, SeekOrigin.Begin);
                return new FileStreamResult(errorStream, "application/pdf")
                {
                    FileDownloadName = "MissingParamsReport.pdf"
                };
            }

            if (report == "PeriodicFiscal")
            {
                parameters.Add(new Parameter() { Name = "enu", Value = enuCode });
                parameters.Add(new Parameter() { Name = "od", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "do", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
            }
            else
            { 
                byte[] errorPdf = new Pdf().GenerateErrorPdf("Trazili ste nepostojeci izvjestaj.");
                var errorStream = new MemoryStream(errorPdf);
                errorStream.Seek(0, SeekOrigin.Begin);
                return new FileStreamResult(errorStream, "application/pdf")
                {
                    FileDownloadName = "InvalidReportType.pdf"
                };
            }

            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var toReport = $"{mnePartnerId}{report}.trdp";
            var rep = _db.Reports.FirstOrDefault(a => a.Name == toReport);
            var path = Path.Combine(_env.ContentRootPath, "Reports", toReport);
            var fileName = string.Empty;

            reportSource.Uri = path;

            try
            {
                reportSource.Parameters.AddRange(parameters); 

                Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);
                
                Response.Headers.Append("Cache-Control", "private, max-age=1800");

                if (result.HasErrors)
                {
                    _logger.LogError("Telerik report - greske za {Report}:", report);
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError(" - {Error}", error.Message);
                    }

                    byte[] errorPdf = new Pdf().GenerateErrorPdf($"Report '{report}' greska pri renderovanju.");

                    var errorStream = new MemoryStream(errorPdf);
                    errorStream.Seek(0, SeekOrigin.Begin);

                    fileName = $"ReportError-{report}.pdf";
                    Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");

                    return new FileStreamResult(errorStream, "application/pdf")
                    {
                        FileDownloadName = fileName
                    };
                }

                var stream = new MemoryStream(result.DocumentBytes);
                stream.Seek(0, SeekOrigin.Begin);
                var fsr = new FileStreamResult(stream, "application/pdf");

                fileName = $"Report-{report}.pdf";
                Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");

                fsr.FileDownloadName = fileName;
                 
                return fsr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception - Telerik report {Report}. Inner: {InnerException}", report, ex.InnerException?.Message);

                byte[] errorPdf = new Pdf().GenerateErrorPdf($"Report '{report}' exception tokom generisanja.\n{ex.Message}");

                var errorStream = new MemoryStream(errorPdf);
                errorStream.Seek(0, SeekOrigin.Begin);

                fileName = $"ReportEx-{report}.pdf";
                Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");

                return new FileStreamResult(errorStream, "application/pdf")
                {
                    FileDownloadName = fileName
                };
            }
        }


    }
}
