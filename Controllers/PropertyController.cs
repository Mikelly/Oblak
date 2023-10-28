using AutoMapper;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Services.MNE;
using System.Diagnostics;

namespace Oblak.Controllers
{
    public class PropertyController : Controller
    {
        private readonly ILogger<PropertyController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public PropertyController(ILogger<PropertyController> logger, ApplicationDbContext db, IMapper mapper)
        {
            _logger = logger;            
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("properties")]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var username = HttpContext.User.Identity.Name;
            var user = _db.Users.FirstOrDefault(a => a.UserName == username);
            var data = _db.Properties.Where(a => a.LegalEntityId == user.LegalEntityId);

            return Json(_mapper.Map<List<PropertyDto>>(data).ToDataSourceResult(request));
        }

    }
}