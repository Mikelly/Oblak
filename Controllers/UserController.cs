using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Models;

namespace Oblak.Controllers
{
    public class UserController : Controller
    {
        ApplicationDbContext _db;
        ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext db, 
            ILogger<UserController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("users")]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = _db.Users.Include(a => a.LegalEntity).Select(a =>
                    new UserViewModel()
                    {
                        Id = a.Id,
                        UserName = a.UserName,
                        Email = a.Email,
                        PhoneNumber = a.PhoneNumber,
                        EfiOperator = a.EfiOperator,
                        LegalEntity = a.LegalEntity.Name,
                        UserCreated = a.UserCreated,
                        UserCreatedDate = a.UserCreatedDate,
                        UserModified = a.UserModified,
                        UserModifiedDate = a.UserModifiedDate,
                    });

            return Json(data.ToDataSourceResult(request));
        }
    }
}
