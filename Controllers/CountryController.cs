using AutoMapper;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace Oblak.Controllers
{
    public class CountryController : Controller
    {
        private readonly ILogger<CountryController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;

        public CountryController(
            ILogger<CountryController> logger, 
            ApplicationDbContext db, 
            IMapper mapper,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _logger = logger;            
            _db = db;
            _mapper = mapper;

            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (username != null)
            {
                _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
                _legalEntityId = _appUser.LegalEntityId;
                _legalEntity = _appUser.LegalEntity;
            }
        }

        [HttpGet]
        [Route("countries")]
        public async Task<IActionResult> Index()
        {
            return View();

        }


        [HttpGet("countries-get-items")]
        public async Task<IActionResult> GetItems(string text)
        {
            var data = await _db.Countries.OrderBy(x => x.CountryName).ToListAsync();

            text = (text ?? "").ToLower();

            var final = data
                .Where(a => a.CountryName.ToLower().Contains(text) || a.CountryName.ToLower().Contains(text))
                .Select(_mapper.Map<CountryDto>)
                //.Take(100)
                .ToList();

            return Json(final);
        }
    }
}