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
    public class ItemController : Controller
    {
        private readonly ILogger<ItemController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ApplicationUser _appUser;
        private readonly int _legalEntityId;        
        private LegalEntity _legalEntity;

        public ItemController(
            ILogger<ItemController> logger, 
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
        [Route("items")]
        public async Task<IActionResult> Index()
        {
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var items = _db.Items.Where(a => a.LegalEntityId == _legalEntity.Id);
                var data = _mapper.Map<List<ItemDto>>(items);
                return Json(await data.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<ActionResult> Update(ItemDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var item = await _db.Items.FindAsync(dto.Id);

                if (item == null)
                {
                    return Json(new DataSourceResult { Errors = "Entity not found." });
                }

                _mapper.Map(dto, item);

                await _db.SaveChangesAsync();

                var items = _db.Items.Where(a => a.LegalEntityId == _legalEntity.Id);
                var data = _mapper.Map<List<ItemDto>>(items);
                return Json(await data.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(ItemDto dto, [DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                var item = new Item();
                _mapper.Map(dto, item);
                item.LegalEntityId = _legalEntity.Id;
                item.Description = item.Name;
                _db.Add(item);
                await _db.SaveChangesAsync();

                var items = _db.Items.Where(a => a.LegalEntityId == _legalEntity.Id);
                var data = _mapper.Map<List<ItemDto>>(items);
                return Json(await data.ToDataSourceResultAsync(request));
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, ItemDto dto)
        {
            try
            {
                var item = _db.Items.Find(dto.Id);                    

                if (item != null)
                {                    
                    _db.Items.Remove(item);
                    _db.SaveChanges();
                    var items = _db.Items.Where(a => a.LegalEntityId == _legalEntity.Id);
                    var data = _mapper.Map<List<ItemDto>>(items);
                    return Json(await data.ToDataSourceResultAsync(request));                    
                }
                else
                {
                    return Json(new { error = "Artikal nije pronađen." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Došlo je do greške prilikom brisanja artikla." });
            }
        }
    }
}