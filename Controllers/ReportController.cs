using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Migrations;
using System.Xml.Linq;
using Telerik.Reporting;

namespace Oblak.Controllers
{
    public class ReportController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReportController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly ApplicationUser _appUser;
        private LegalEntity _legalEntity;

        public ReportController(
            IWebHostEnvironment env,
            ILogger<ReportController> logger,
            ApplicationDbContext db,
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
                object v = type switch {
                    "String" => formValue.ToString(),
                    "Integer" => formValue != null ? int.Parse(formValue.ToString()) : value,
                    _ => ""
                };
            }
        }

        [HttpGet]
        [Route("reports-tourist-org")]
        public ActionResult TouristOrg()
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

            return View();
        }


        [HttpPost]
        [Route("/treport")]
        public async Task<FileResult> TouristOrgRunReport()
        {
            var report = Request.Form["Report"];
            var format = Request.Form["Format"];
            var date = Request.Form["Date"];
            var dateFrom = Request.Form["DateFrom"];
            var dateTo = Request.Form["DateTo"];
            var checkInPoint = Request.Form["CheckInPointId"];
            var username = Request.Form["UserName"];

            //Dictionary<string, object> parameters = new Dictionary<string, object>();
            List<Parameter> parameters = new List<Parameter>();

            if (report == "CountryStats")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "Date", Value = DateTime.ParseExact(date, "ddMMyyyy", null) });
            }
            else if (report == "CountryStatsPeriod")
            {
                parameters.Add(new Parameter() { Name = "Partner", Value = _legalEntity.PartnerId });
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
            else if (report == "ResTax")
            {
                parameters.Add(new Parameter() { Name = "PartnerId", Value = _legalEntity.PartnerId });
                parameters.Add(new Parameter() { Name = "DateFrom", Value = DateTime.ParseExact(dateFrom, "ddMMyyyy", null) });
                parameters.Add(new Parameter() { Name = "DateTo", Value = DateTime.ParseExact(dateTo, "ddMMyyyy", null) });
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

            var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            var deviceInfo = new System.Collections.Hashtable();
            var reportSource = new Telerik.Reporting.UriReportSource();

            var toReport = $"{_legalEntity.PartnerId}{report}.trdp";
            var rep = _db.Reports.FirstOrDefault(a => a.Name == toReport);
            var path = Path.Combine(_env.ContentRootPath, "Reports", toReport);

            reportSource.Uri = path;

            reportSource.Parameters.AddRange(parameters);
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport(format, reportSource, deviceInfo);

            if (!result.HasErrors)
            {
                return File(result.DocumentBytes, "application/pdf");
            }

            return null;
        }
    }
}
