using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Models;
using Oblak.Models.Account;
using Oblak.Models.Api;
using Oblak.Services;
using Oblak.Services.MNE;
using Oblak.Services.SRB;

namespace Oblak.Controllers;

public class UserController : Controller
{
    private readonly ILogger<UserController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ApplicationUser _appUser;
    private readonly int _legalEntityId;        
    private LegalEntity _legalEntity;
    private readonly UserManager<IdentityUser> _userManager;

    public UserController(
        ILogger<UserController> logger, 
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
            _legalEntityId = _appUser.LegalEntityId;
            _legalEntity = _appUser.LegalEntity;
        }
    }

    [HttpGet]
    [Route("users")]
    public async Task<IActionResult> Users()
    {
        if (User.IsInRole("TouristOrgAdmin"))
        {
            ViewBag.CPS = new SelectList(_db.CheckInPoints.Where(a => a.PartnerId == _appUser.PartnerId), "Id", "Name");
            ViewBag.TPS = new SelectList(Enum.GetNames(typeof(UserType)).ToArray().Where(a => a.StartsWith("Tourist")).Select(a => new { Id = a, Name = a }), "Id", "Name");
            
            return View("ToUsers");
        }    

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
    {
        var partner = _legalEntity.PartnerId;
        var users = _db.Users.Where(a => a.PartnerId == partner).Select(a => new UserDto { 
            Id = a.Id,
            UserName = a.UserName,
            Email = a.Email,
            PersonName = a.PersonName,
            PartnerId = a.PartnerId,
            LegalEntityId = a.LegalEntityId,
            Type = a.Type.ToString(),
            CheckInPointId = a.CheckInPointId,
            IsCertRequired = a.IsCertRequired,
            IsActive = a.LockoutEnd == null || a.LockoutEnd <= DateTimeOffset.UtcNow
        });

        return Json(await users.ToDataSourceResultAsync(request));
    }

    [HttpPost]
    public async Task<ActionResult> Update(UserDto dto, [DataSourceRequest] DataSourceRequest request)
    {
        try
        {
            var user = await _db.Users.FindAsync(dto.Id);

            if (user == null)
            {
                return Json(new DataSourceResult { Errors = "Entity not found." });
            }

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.PersonName = dto.PersonName;
            user.Type = (UserType)Enum.Parse(typeof(UserType), dto.Type);
            user.CheckInPointId = dto.CheckInPointId;   
            user.IsCertRequired = dto.IsCertRequired;

            await _db.SaveChangesAsync();
                        
            return Json(new[] { _mapper.Map(user, dto) }.ToDataSourceResult(request, ModelState));
        }
        catch (Exception ex)
        {
            return Json(new DataSourceResult { Errors = ex.Message });
        }
    }    

    [HttpPost]
    public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, UserDto dto)
    {
        try
        {
            var u = _db.Users.Find(dto.Id);                    

            if (u != null)
            {                    
                _db.Users.Remove(u);
                _db.SaveChanges();
                var users = _db.Users.Where(a => a.PartnerId == _legalEntity.PartnerId).Select(a => new UserDto
                {
                    Id = a.Id,
                    UserName = a.UserName,
                    Email = a.Email,
                    PersonName = a.PersonName,
                    PartnerId = a.PartnerId,
                    LegalEntityId = a.LegalEntityId,
                    Type = a.Type.ToString(),
                    CheckInPointId = a.CheckInPointId,
                    IsCertRequired = dto.IsCertRequired,
                });

                return Json(await new[] { u }.ToDataSourceResultAsync(request));
            }
            else
            {
                return Json(new { error = "Korisnik nije pronađen." });
            }
        }
        catch (Exception ex)
        {
            return Json(new { error = "Došlo je do greške prilikom brisanja punkta." });
        }
    }

    [HttpGet]
    [Route("tourist-org-reset-password")]
    public async Task<ActionResult> ResetPassword(string userid)
    {
        var user = _db.Users.FirstOrDefault(a => a.Id == userid);

        ViewBag.UserId = userid;

        if (user != null)
        {
            var appUser = await _userManager.FindByNameAsync(user.UserName);
            ViewBag.User = user;
            ViewBag.Locked = await _userManager.IsLockedOutAsync(appUser);
        }
        else
        {
            ViewBag.Locked = false;
        }

        return PartialView();
    } 

    [HttpPost]
    [Route("tourist-org-reset-password")]
    public async Task<ActionResult> ResetPassword(string userid, string password)
    {
        try
        {
            var user = _db.Users.FirstOrDefault(a => a.Id == userid);
            user.LockoutEnd = null;
            _db.SaveChanges();

            string code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, code, password);

            return Json(new BasicDto() { error = "", errors = null, info = "Uspješno resetovana lozinka!" });
        }
        catch (Exception ex)
        {
            return Json(new BasicDto() { error = ex.Message, errors = null, info = "" });
        }
    }

    [HttpGet]
    [Route("tourist-org-manage-user")]
    public async Task<ActionResult> ManageUser(string userid)
    {
        var user = _db.Users.FirstOrDefault(a => a.Id == userid);

        ViewBag.UserId = userid;

        if (user != null)
        {
            var appUser = await _userManager.FindByNameAsync(user.UserName);
            ViewBag.User = user;
            ViewBag.Locked = await _userManager.IsLockedOutAsync(appUser);
        }
        else
        {
            ViewBag.Locked = false;
        }

        return PartialView();
    }

    [HttpPost("turist-org-user-lock-unlock")]
    public async Task<IActionResult> UserLockUnlock([FromBody] LockUnlockModel model)
    {
        if (!model.Lock.HasValue)
            return BadRequest(new
            {
                success = false,
                error = "Parametar 'lock' nedostaje!"
            });
         
        var user = _db.Users.Find(model.EntityId);
        if (user == null)
            return NotFound(new
            {
                success = false,
                error = "Korisnik nije pronadjen!"
            });

        if (model.Lock.Value)
        {
            var setLockoutEnabledResult = await _userManager.SetLockoutEnabledAsync(user, true);
            if (!setLockoutEnabledResult.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    error = "<div>Zakljucavanje korisnika nije uspelo!</div>" + string.Join("", setLockoutEnabledResult.Errors.Select(e => $"<div>{e.Description}</div>"))

                });

            var setLockoutEndDateResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (!setLockoutEndDateResult.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    error = "<div>Zakljucavanje korisnika nije uspelo!</div>" + string.Join("", setLockoutEndDateResult.Errors.Select(e => $"<div>{e.Description}</div>"))

                });

            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    error = "<div>Neuspjesno azuriranje security stamp-a!</div>" + string.Join("", stampResult.Errors.Select(e => $"<div>{e.Description}</div>"))
                });

            return Ok(new
            {
                success = true,
                error = "Korisnik je zaključan i trenutno nema pristup aplikaciji."
            });
        }
        else
        {
            var setLockoutEndDateResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            if (!setLockoutEndDateResult.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    error = "<div>Zakljucavanje korisnika nije uspelo!</div>" + string.Join("", setLockoutEndDateResult.Errors.Select(e => $"<div>{e.Description}</div>"))

                });

            var setLockoutEnabledResult = await _userManager.SetLockoutEnabledAsync(user, false);
            if (!setLockoutEnabledResult.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    error = "<div>Zakljucavanje korisnika nije uspelo!</div>" + string.Join("", setLockoutEnabledResult.Errors.Select(e => $"<div>{e.Description}</div>"))

                });

            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    error = "<div>Neuspjesno azuriranje security stamp-a!</div>" + string.Join("", stampResult.Errors.Select(e => $"<div>{e.Description}</div>"))
                });


            return Ok(new
            {
                success = true,
                error = "Korisnik je otključan i sada može ponovo da se uloguje."
            });
        }
    }

    [HttpPost("turist-org-user-reset-password")]

    public async Task<IActionResult> UserResetPassword([FromBody] ResetPasswordModel model)
    {
        if (string.IsNullOrEmpty(model.NewPassword))
            return BadRequest(new
            {
                success = false,
                error = "Nova lozinka je obavezna."
            }); 

        var user = _db.Users.Find(model.EntityId);

        if (user == null)
            return NotFound(new
            {
                success = false,
                error = "Korisnik nije pronadjen!"
            });

        string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetPasswordResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

        if (!resetPasswordResult.Succeeded)
            return BadRequest(new
            {
                success = false,
                error = "<div>Resetovanje lozinke nije uspelo!</div>" + string.Join("", resetPasswordResult.Errors.Select(e => $"<div>{e.Description}</div>"))

            });

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);

        if (!stampResult.Succeeded)
            return BadRequest(new
            {
                success = false,
                error = "<div>Neuspjesno azuriranje security stamp-a!</div>" + string.Join("", stampResult.Errors.Select(e => $"<div>{e.Description}</div>"))
            });

        return Ok(new
        {
            success = true,
            error = "<div>Lozinka je resetovana.</div> <div>Korisnik ce morati ponovo da se uloguje.</div>"
        });
    }
}