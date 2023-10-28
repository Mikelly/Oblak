using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Models;

namespace Oblak.Controllers
{
    public class SelfRegisterController : Controller
    {
        ApplicationDbContext _db;
        ILogger<SelfRegisterController> _logger;

        public SelfRegisterController(
            ApplicationDbContext db, 
            ILogger<SelfRegisterController> logger)
        {
            _db = db;
            _logger = logger;
        }
    }
}
