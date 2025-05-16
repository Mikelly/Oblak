using AutoMapper;
using AutoMapper.QueryableExtensions;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Models.Computer;

namespace Oblak.Controllers;
 
public class ComputerController : Controller
{
    private readonly ILogger<ComputerController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ApplicationUser _appUser;
    private readonly int? _partnerId;      
    private readonly UserManager<IdentityUser> _userManager;

    public ComputerController(
        ILogger<ComputerController> logger, 
        ApplicationDbContext db, 
        IMapper mapper,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager
        )
    {
        _logger = logger;            
        _db = db;
        _mapper = mapper;
        _userManager = userManager;

        var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (username != null)
        {
            _appUser = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.Properties).FirstOrDefault(a => a.UserName == username)!;
            _partnerId = _appUser.PartnerId; 
        }
    }

    [HttpGet("computers")]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("TouristOrgAdmin") && _appUser.PartnerId == 4)
        {  
            return View();
        }

        return base.RedirectToAction("Index", "Home");
    }
      
    [HttpPost]
    public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
    {  
        if(!_partnerId.HasValue) 
            return Json(new DataSourceResult { Errors = "Niste registrovani!" });
            
        var computerDtos = await _db.Computers
                                    .Where(a => a.PartnerId == _partnerId)
                                    .OrderByDescending(a => a.UserCreatedDate)
                                    .ProjectTo<ComputerDto>(_mapper.ConfigurationProvider)
                                    .ToListAsync();

        return Json(await computerDtos.ToDataSourceResultAsync(request));
    }

    [HttpPost]
    public async Task<ActionResult> Create(ComputerDto input, [DataSourceRequest] DataSourceRequest request)
    {
        try
        {
            var errors = string.Empty;

            if (!ModelState.IsValid)
                errors += "Podaci nisu validni!" + Environment.NewLine;

            if (!_partnerId.HasValue)
                errors += "Niste registrovani!" + Environment.NewLine;

            if (errors != string.Empty) return Json(new DataSourceResult { Errors = errors });
            else
            {
                var model = new Computer
                {
                    PartnerId = (int)_partnerId,
                    PCName = input.PCName,
                    LocationDescription = input.LocationDescription,
                };

                await _db.Computers.AddAsync(model);
                _db.SaveChanges(); 

                var data = await _db.Computers.Where(x => x.PartnerId == _partnerId).OrderByDescending(x => x.UserCreatedDate).ToListAsync();
                var computers = _mapper.Map<List<ComputerDto>>(data);
                return Json(await computers.ToDataSourceResultAsync(request));
            } 
        }
        catch (Exception ex)
        {
            return Json(new DataSourceResult { Errors = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult> Update(ComputerDto dto, [DataSourceRequest] DataSourceRequest request)
    {
        try
        {
            var computer = await _db.Computers.FindAsync(dto.Id);

            if (computer == null)
            {
                return Json(new DataSourceResult { Errors = "Entitet nije pronadjen!" });
            }

            if (!_partnerId.HasValue)
            {
                return Json(new DataSourceResult { Errors = "Niste registrovani!" });
            }

            computer.PCName = dto.PCName;
            computer.LocationDescription = dto.LocationDescription;
              
            _db.SaveChanges();
                        
            return Json(new[] { _mapper.Map(computer, dto) }.ToDataSourceResult(request, ModelState));
        }
        catch (Exception ex)
        {
            return Json(new DataSourceResult { Errors = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult> Delete(ComputerDto dto, [DataSourceRequest] DataSourceRequest request)
    {
        try
        {
            var computer = _db.Computers.Find(dto.Id);                    

            if (computer != null)
            {                    
                _db.Computers.Remove(computer);
                _db.SaveChanges();
                  
                return Json(await new[] { computer }.ToDataSourceResultAsync(request));
            }
            else
            {
                return Json(new { error = "Korisnik nije pronađen." });
            }
        }
        catch (Exception ex)
        {
            return Json(new { error = "Došlo je do greške prilikom brisanja računara." });
        }
    }

    [HttpGet]
    [Route("computer-reset")]
    public async Task<IActionResult> ResetComputer(Guid computerId)
    {
        var computer = _db.Computers.FirstOrDefault(c => c.Id == computerId);
        if (computer == null)
        {
            return Json(new { success = false, message = "Računar nije pronađen" });
        }
         
        computer.Registered = null;
        computer.UserRegistered = null;
        computer.Logged = null;
        computer.UserLogged = null;
          
        _db.SaveChanges();

        return Json(new { success = true, message = "Registracija je ponovo omogućena, pri prvom narednom login-u korisnika." });
    }

    [HttpGet]
    public IActionResult ShowLogs(Guid computerId)
    { 
        ViewBag.ComputerId = computerId; 
        return PartialView("_ComputerLogsPartial");
    }

    [HttpPost]
    public async Task<IActionResult> ReadLogs(Guid computerId, DateTime? from, DateTime? to, [DataSourceRequest] DataSourceRequest request)
    {  
        var oneWeekAgo = DateTime.Now.AddDays(-15); 
        var logs = _db.ComputerLogs.Where(l => l.ComputerId == computerId);
         
        if (from.HasValue)
            logs = logs.Where(l => l.Seen >= from.Value);

        if (to.HasValue)
            logs = logs.Where(l => l.Seen <= to.Value);

        if (!from.HasValue && !to.HasValue)
            logs = logs.Where(l => l.Seen >= oneWeekAgo);
         
        return Json(await logs.OrderByDescending(l => l.Seen).ToList().ToDataSourceResultAsync(request));
    }

    [HttpPost]
    public ActionResult DeleteLogs(Guid computerId)
    {
        var logs = _db.ComputerLogs.Where(l => l.ComputerId == computerId);
        _db.ComputerLogs.RemoveRange(logs);
        _db.SaveChanges();

        return Json(new { success = true });
    }

}