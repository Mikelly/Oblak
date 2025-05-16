using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Computer;
using Oblak.Services.MNE;
using System.Diagnostics;

namespace Oblak.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private MneClient _client;
        private readonly ApplicationDbContext _db;
        private readonly ApplicationUser _appUser;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger,
            MneClient client,
            ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _logger = logger;
            _client = client;
            _db = db;
            _configuration = configuration;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
            }
        }


        [HttpGet]
        [Route("home", Name = "Home")]
        [Route("/", Name = "Root")]
        public IActionResult Index()
        {
            bool bOk = true;

            if (User.Identity?.IsAuthenticated == true && _appUser != null && _appUser.PartnerId == 4)
            {
                if (Request.Cookies.TryGetValue("device_id", out string deviceIdValue))
                {
                    if (Guid.TryParse(deviceIdValue, out Guid deviceId))
                    {
                        var computer = _db.Computers.FirstOrDefault(c => c.Id == deviceId);

                        if (computer == null || (computer != null && computer.Registered == null))
                        {
                            Response.Cookies.Delete("device_id");
                            bOk = false;
                        }
                    }
                    else
                    {
                        Response.Cookies.Delete("device_id");
                        bOk = false;
                    }
                }
                else
                {
                    bOk = false;
                }
            }

            ViewBag.IsComputerRegistered = bOk;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("/available-computers")]
        public IActionResult AvailableComputers()
        {
            if (!(User.Identity?.IsAuthenticated == true && _appUser != null && _appUser.PartnerId == 4))
                return Json(new { error = "Nemate pravo na ovaj resurs." });

            var computers = _db.Computers
                                .Where(c => c.Registered == null
                                         || c.PCName == "Administracija"
                                         || c.PCName == "Privremeni")
                                .OrderBy(c => c.PCName)
                                .ToList();

            if (!computers.Any())
            {
                return Json(new { error = "Ovaj racunar nije registrovan." });
            }

            return PartialView("_RegistrationComputer", computers);
        }

        [HttpPost("/registration-computer")]
        public async Task<IActionResult> RegistrationComputer(RegistrationComputerViewModel request)
        {
            if (!request.Id.HasValue)
                return Json(new { error = "Nevalidan unos podataka pri registraciji računara." });

            if (!(User.Identity?.IsAuthenticated == true && _appUser != null && _appUser.PartnerId == 4))
                return Json(new { error = "Nemate pravo da registrujete ovaj računar." });

            var computer = await _db.Computers.FindAsync(request.Id);
            if (computer == null)
                return Json(new { error = "Nepostojeci racunar." });

            if (computer.Registered != null && computer.PCName != "Administracija" && computer.PCName != "Privremeni")
                return Json(new { error = "Odabrani racunar je vec registrovan!" });

            var adminPin = _configuration["ComputerPINs:Administracija"];
            var tempPin = _configuration["ComputerPINs:Privremeni"];

            string requiredPin = null;
            if (computer.PCName == "Administracija")
                requiredPin = adminPin;
            else if (computer.PCName == "Privremeni")
                requiredPin = tempPin;

            if (!string.IsNullOrEmpty(requiredPin) && request.PIN != requiredPin)
                return Json(new { error = "Neispravan PIN kod!" });

            computer.Registered = computer.Logged = DateTime.Now;
            computer.UserRegistered = computer.UserLogged = User.Identity?.Name;

            if (computer.PCName != "Administracija")
            {
                string ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                }

                ComputerLog log = new ComputerLog
                {
                    ComputerId = computer.Id,
                    Action = "Registracija računara",
                    BrowserName = request.BrowserName,
                    OSName = request.OSName,
                    ScreenResolution = request.ScreenResolution,
                    IsMobile = request.IsMobile,
                    Seen = DateTime.Now,
                    UsedByUser = User.Identity?.Name,
                    TimeZone = request.TimeZone,
                    UserAgent = request.UserAgent,
                    IPAddress = ipAddress,
                };
                _db.ComputerLogs.Add(log);
            }
            else
            {
                computer.Logged = null;
                computer.UserLogged = null;
            }

                _db.SaveChanges();

            Response.Cookies.Append("device_id", computer.Id.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(5),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Json(new { success = true });
        }

    }
}