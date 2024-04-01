using Microsoft.AspNetCore.Mvc;
using Oblak.Models;
using Oblak.Services.MNE;
using System.Diagnostics;

namespace Oblak.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private MneClient _client;

        public HomeController(ILogger<HomeController> logger, MneClient client)
        {
            _logger = logger;
            _client = client;
        }

<<<<<<< HEAD
        [HttpGet]
=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        [Route("home", Name = "Home")]
        [Route("/", Name = "Root")]
        public IActionResult Index()
        {
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
    }
}