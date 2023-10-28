using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Oblak.Data;
using Oblak.Models.Account;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Oblak.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration configuration, 
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _configuration = configuration;
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        private void AddPasswordErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                if (error.Code == "PasswordTooShort")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora biti duga barem 8 karaktera.");
                }
                else if (error.Code == "PasswordRequiresNonAlphanumeric")
                {
                    ModelState.AddModelError(string.Empty, "Loznika mora imati barem jedan specijalan karakter.");
                }
                else if (error.Code == "PasswordRequiresDigit")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora imati barem jedan broj ('0'-'9').");
                }
                else if (error.Code == "PasswordRequiresLower")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora imati barem jedno malo slovo ('a'-'z').");
                }
                else if (error.Code == "PasswordRequiresUpper")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora imati barem jedno veliko slovo slovo. ('A'-'Z').");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }


        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("sign-in", Name = "signIn")]
        public IActionResult SignIn(string? returnUrl = null)
        {
            var model = new SignInViewModel();

            ViewData["ReturnUrl"] = returnUrl;            

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]        
        [Route("sign-in", Name = "signIn")]
        public async Task<IActionResult> SignIn(SignInViewModel model, string? returnUrl = null)
        {
            bool loginCodeRequired = false;

            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.UserName);
                if(user == null) await _userManager.FindByNameAsync(model.UserName);

                if (user != null)
                {
                    if (!user.EmailConfirmed && false)
                    {
                        return RedirectToRoute("CompanyResendVerification", new { id = user.Id });
                    }

                    var checkPassword = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

                    if (checkPassword.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, true);
                        return RedirectToAction("Auth");
                    }

                    if (checkPassword.IsLockedOut)
                    {
                        _logger.LogWarning(2, "User account locked out.");
                        return View("Lockout");
                    }
                }

                _logger.LogInformation(1, "Neuspješan pokusaj prijave.");

                ModelState.AddModelError(string.Empty, "Neuspješan pokusaj prijave.");
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        [Route("sign-out", Name = "SignOut")]
        public async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();           
            return RedirectToAction("Auth");
        }


        [HttpGet("register")]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid == false)
            {
                if (_db.LegalEntities.Any(a => a.TIN == model.LegalEntityTIN))
                {
                    ModelState.AddModelError("LegalEntityTIN", "Već postoji pravni subjekat sa unesenim poreskim brojem");
                }
                if (_db.Users.Any(a => a.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Već postoji korisnik sa unesenom e-mail adresom");
                }
                if (_db.Users.Any(a => a.Email == model.UserName))
                {
                    ModelState.AddModelError("UserName", "Već postoji korisnik sa unesenim korisničkim imenom");
                }
            }
            else
            {
                var le = new LegalEntity();
                le.TIN = model.LegalEntityTIN;
                le.InVat = model.LegalEntityInVat;
                le.Address = model.LegalEntityAddress;
                le.Country = model.Country;
                le.Name = model.LegalEntityName;
                _db.LegalEntities.Add(le);
                _db.SaveChanges();

                var result = await _userManager.CreateAsync(new ApplicationUser() { 
                        UserName = model.UserName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        LegalEntityId = le.Id
                    }, model.Password);

                if (result.Succeeded)
                {
                    return Ok("Sve okej");
                }
                else
                {
                    AddPasswordErrors(result);
                }
            }

            return View();
        }

        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
            var _TokenExpiryTimeInHour = Convert.ToInt64(_configuration["JWT:TokenExpiryTimeInHour"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _configuration["JWT:ValidIssuer"],
                Audience = _configuration["JWT:ValidAudience"],
                Expires = DateTime.UtcNow.AddHours(_TokenExpiryTimeInHour),                
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(claims)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpGet("check-auth")]
        public IActionResult Auth()
        {
            return Ok(HttpContext.User.Identity!.IsAuthenticated);
        }
    }
}
