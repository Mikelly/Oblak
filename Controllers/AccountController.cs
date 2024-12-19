using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Models.Account;
using Oblak.Models.Api;
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
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration configuration,
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager)
        {
            _logger = logger;
            _configuration = configuration;
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        private string AddPasswordErrors(IdentityResult result)
        {
            var errors = string.Empty;
            foreach (var error in result.Errors)
            {
                if (error.Code == "PasswordTooShort")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora biti duga barem 8 karaktera.");
                    errors += "Lozinka mora biti duga barem 8 karaktera." + Environment.NewLine;
                }
                else if (error.Code == "PasswordRequiresNonAlphanumeric")
                {
                    ModelState.AddModelError(string.Empty, "Loznika mora imati barem jedan specijalan karakter.");
                    errors += "Loznika mora imati barem jedan specijalan karakter." + Environment.NewLine;
                }
                else if (error.Code == "PasswordRequiresDigit")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora imati barem jedan broj ('0'-'9').");
                    errors += "Lozinka mora imati barem jedan broj ('0'-'9')." + Environment.NewLine;
                }
                else if (error.Code == "PasswordRequiresLower")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora imati barem jedno malo slovo ('a'-'z').");
                    errors += "Lozinka mora imati barem jedno malo slovo ('a'-'z')." + Environment.NewLine;
                }
                else if (error.Code == "PasswordRequiresUpper")
                {
                    ModelState.AddModelError(string.Empty, "Lozinka mora imati barem jedno veliko slovo slovo. ('A'-'Z').");
                    errors += "Lozinka mora imati barem jedno veliko slovo slovo. ('A'-'Z')." + Environment.NewLine;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    errors += error.Description + Environment.NewLine;
                }
            }
            return errors;
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

            ViewBag.BaseUrl = HttpContext.Request.BaseUrl();

            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.Error = "";

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
                if (user == null) user = await _userManager.FindByNameAsync(model.UserName.ToUpper());

                if (user != null)
                {
                    if (!user.EmailConfirmed && false)
                    {
                        return RedirectToRoute("CompanyResendVerification", new { id = user.Id });
                    }

                    var checkPassword = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

                    if (checkPassword.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, true);
                        if (User.IsInRole("TouristOrgOperator"))
                        {
                            var au = _db.Users.Include(a => a.LegalEntity).ThenInclude(a => a.PassThrough).FirstOrDefault(u => u.Id == user.Id);
                            if (au.LegalEntity.PassThroughId != null)
                            {
                                _db.Logs.Add(new Log() { Action = "Login", LegalEntityId = au.LegalEntity.PassThroughId.Value, PartnerId = au.LegalEntity.PassThrough.PartnerId.Value, UserName = user.UserName });
                                _db.SaveChanges();
                            }
                        }
                        return RedirectToRoute("Home");
                    }

                    if (checkPassword.IsLockedOut)
                    {
                        _logger.LogWarning(2, "User account locked out.");
                        return View("Lockout");
                    }
                }

                _logger.LogInformation(1, "Neuspješan pokušaj prijave.");

                ModelState.AddModelError(string.Empty, "Neuspješan pokušaj prijave.");
                ViewBag.Error = "Neuspješan pokušaj prijave.";
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        [Route("sign-out", Name = "SignOut")]
        public async Task<IActionResult> SignOut()
        {
			if (User.IsInRole("TouristOrgOperator"))
			{
				var au = _db.Users.FirstOrDefault(u => u.UserName.ToLower() == User.Identity.Name.ToLower());
				if (au.PartnerId.HasValue)
				{
					_db.Logs.Add(new Log() { Action = "Logout", LegalEntityId = au.LegalEntityId, PartnerId = au.PartnerId.Value });
				}
			}
            await _signInManager.SignOutAsync();
			return RedirectToAction("SignIn");
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
                var passwordValidator = new PasswordValidator<IdentityUser>();
                var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, null, model.Password);

                if (passwordValidationResult.Succeeded == false)
                {
                    AddPasswordErrors(passwordValidationResult);
                    return View();
                }

                var le = new LegalEntity();
                le.TIN = model.LegalEntityTIN;
                le.InVat = model.LegalEntityInVat;
                le.Address = model.LegalEntityAddress;
                le.Country = model.Country;
                le.Type = model.LegalEntityType == "Person" ? Data.Enums.LegalEntityType.Person : Data.Enums.LegalEntityType.Company;
                le.Name = model.LegalEntityName;
                _db.LegalEntities.Add(le);
                _db.SaveChanges();

                if (model.Reference != null)
                {
                    var prtn = _db.Partners.Where(a => a.Reference == model.Reference).FirstOrDefault();
                    if (prtn != null)
                    {
                        le.PartnerId = prtn.Id;
                        _db.SaveChanges();
                    }
                }

                var result = await _userManager.CreateAsync(new ApplicationUser()
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    LegalEntityId = le.Id
                }, model.Password);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);
                    }

                    return Ok("Sve okej");
                }
                else
                {
                    _db.LegalEntities.Remove(le);
                    _db.SaveChanges();
                    AddPasswordErrors(result);
                }
            }

            return View();
        }

        public async Task<bool> IsPasswordValidAsync(UserManager<IdentityUser> userManager, IdentityUser user, string password)
        {
            // Loop through all password validators configured in Identity
            foreach (var validator in userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(userManager, user, password);
                if (!result.Succeeded)
                {
                    // If validation fails, return false or handle errors as needed
                    return false;
                }
            }
            return true; // All validations passed
        }

        [HttpGet("change-password")]
        public async Task<IActionResult> ChangePasswordView()
        { 
            return View("ChangePassword");
        }

        [HttpPost("do-change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the currently authenticated user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User is not logged in.");
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return Ok(new { error = "Nova loznika i potvrda se ne poklapaju!" });
            }

            if ((await IsPasswordValidAsync(_userManager, user, model.NewPassword)) == false)
            {
                return Ok(new { error = "Nova loznika ne zadovoljava uslove (minimum 8 karaktera, veliko i malo slovo, broj i specijalni karakter)!" });
            }

            // Change the password
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var r in result.Errors)
                { 
                    if(r.Code == "PasswordMismatch") return Ok(new { error = "Netačna stara lozinka!" });
                }

                // Return the error details
                return BadRequest(result.Errors);
            }

            // Refresh the sign-in to update authentication cookies
            await _signInManager.RefreshSignInAsync(user);

            return Ok(new { info = "Lozinka upješno promijenjena!" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string password)
        {

            var user = await _userManager.FindByEmailAsync("");
            if (user == null)
            {            
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                // Handle error (user might not have a password set)
            }
            
            var setPasswordResult = await _userManager.AddPasswordAsync(user, password);
            if (!setPasswordResult.Succeeded)
            {
                // Handle errors if needed
            }

            return RedirectToAction("ResetPasswordConfirmation");
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

        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole(string name)
        {
            var newRole = new IdentityRole() { Name = name };
            await _roleManager.CreateAsync(newRole);
            //await _roleManager.UpdateAsync(newRole);

            return Ok();
        }

        [HttpPost("add-role-to-user")]
        public async Task<IActionResult> AddRoleToUser(string roleName, string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            await _userManager.AddToRoleAsync(user, roleName);

            return Ok();
        }



        [HttpPost("remove-role-from-user")]
        public async Task<IActionResult> RemoveRoleFromUser(string roleName, string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            await _userManager.RemoveFromRoleAsync(user, roleName);

            return Ok();
        }

        [HttpGet("roles-admin")]
        public async Task<IActionResult> RolesAdmin(int? legalEntity, string username)
        {
            var userName = string.Empty;

            if (legalEntity.HasValue)
            { 
                userName = _db.Users.Where(a => a.LegalEntityId == legalEntity).Select(a => a.UserName).FirstOrDefault();
            }

            if (username != null)
            { 
                userName = username.Trim();
            }

            if (userName != null)
            {
                var user = await _userManager.FindByNameAsync(userName);
                var userRoles = await _userManager.GetRolesAsync(user);
                var roles = _roleManager.Roles.ToList();

                if (await _userManager.IsInRoleAsync(user, "ADMINISTRATOR") == false)
                {
                    roles = roles.Where(a => a.NormalizedName != "PARTNER" && a.NormalizedName != "ADMINISTRATOR").ToList();
                }

                var data = roles.Select(a => new { a.Name, HasRole = userRoles.Contains(a.Name) }).ToDictionary(a => a.Name!, b => b.HasRole);

                ViewBag.UserName = userName;
                ViewBag.Roles = data;

                return PartialView();
            }
            else
            {
                return Json(new BasicDto() { info = "", error = "Ne možete administrirati korisničke uloge, prije nego što napravite korisnički nalog za izdavaoca!" });
            }
        }

        [HttpGet("roles-user")]
        public async Task<IActionResult> RolesUser(string username)
        {            
            if (username != null)
            {
                var user = await _userManager.FindByNameAsync(username);
                var userRoles = await _userManager.GetRolesAsync(user);
                var roles = _roleManager.Roles.ToList();

                if (await _userManager.IsInRoleAsync(user, "ADMINISTRATOR") == false)
                {
                    roles = roles.Where(a => a.NormalizedName != "PARTNER" && a.NormalizedName != "ADMINISTRATOR").ToList();
                }

                var data = roles.Select(a => new { a.Name, HasRole = userRoles.Contains(a.Name) }).ToDictionary(a => a.Name!, b => b.HasRole);

                ViewBag.UserName = username;
                ViewBag.Roles = data;

                return PartialView();
            }
            else
            {
                return Json(new BasicDto() { info = "", error = "Ne možete administrirati korisničke uloge, prije nego što napravite korisnički nalog za izdavaoca!" });
            }
        }


        [HttpGet("legal-entity-account")]
        public async Task<IActionResult> LegalEntityAccount(int legalEntity)
        {
            var le = _db.LegalEntities.Find(legalEntity);

            var user = _db.Users.FirstOrDefault(a => a.LegalEntityId == le.Id);

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

        [HttpGet("tourist-org-account")]
        public async Task<IActionResult> TouristOrgAccount()
        {
            var username = User.Identity.Name;
            var _appUser = _db.Users.FirstOrDefault(a => a.UserName == username);

            ViewBag.CPS = new SelectList(_db.CheckInPoints.Where(a => a.PartnerId == _appUser.PartnerId), "Id", "Name");
            ViewBag.TPS = new SelectList(Enum.GetNames(typeof(UserType)).ToArray().Where(a => a.StartsWith("Tourist")).Select(a => new { Id = a, Name = a }), "Id", "Name");
            return PartialView();
        }

        [HttpPost("legal-entity-account")]
        public async Task<IActionResult> LegalEntityAccount(int legalEntity, AccountViewModel model)
        {
            var le = _db.LegalEntities.Find(legalEntity);

            var user = _db.Users.FirstOrDefault(a => a.LegalEntityId == le.Id);

            if (user == null)
            {
                var errors = string.Empty;
                if (_db.Users.Any(a => a.Email == model.Email))
                {
                    errors += "Već postoji korisnik sa unesenom e-mail adresom!" + Environment.NewLine;
                }
                if (_db.Users.Any(a => a.UserName == model.UserName))
                {
                    errors += "Već postoji korisnik sa unesenim korisničkim imenom!" + Environment.NewLine;
                }
                if (model.ConfirmPassword != model.Password)
                {
                    errors += "Lozinka i potvrda lozinke se ne poklapaju!" + Environment.NewLine;
                }

                var passwordValidator = new PasswordValidator<IdentityUser>();
                var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, null, model.Password);
                errors += AddPasswordErrors(passwordValidationResult);

                if (errors != string.Empty) return Json(new BasicDto() { error = errors, info = "" });
                else
                {
                    var result = await _userManager.CreateAsync(new ApplicationUser()
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        LegalEntityId = le.Id
                    }, model.Password);

                    if (result.Succeeded)
                    {
                        user = _db.Users.FirstOrDefault(a => a.UserName == model.UserName);
                        if (user != null)
                        {
                            user.EmailConfirmed = true;
                            user.LegalEntityId = legalEntity;
                            _db.SaveChanges();
                        }

                        return Json(new BasicDto() { error = "", info = "Uspješno kreiran nalog!" });
                    }
                    else
                    {
                    }
                }
            }
            else
            {
                return Json(new BasicDto() { error = "Već postoji korisnički nalog za izdavaoca", info = "" });
            }

            return Ok();
        }

        [HttpPost("tourist-org-account")]
        public async Task<IActionResult> TouristOrgAccount(AccountViewModel model)
        {
            var errors = string.Empty;
            if (model.UserName == null)
            {
                errors += "Morate unijeti korisničko ime!" + Environment.NewLine;
            }
            if (model.Password == null || model.ConfirmPassword == null)
            {
                errors += "Morate unijeti lozinku i potvrdu lozinke!" + Environment.NewLine;
            }
            if (model.Email == null)
            {
                errors += "Morate unijeti email adresu!" + Environment.NewLine;
            }
            if (model.Type == null)
            {
                errors += "Morate unijeti vrstu korisnika!" + Environment.NewLine;
            }
            if (model.CheckInPointId == null)
            {
                errors += "Morate unijeti Check-In Point!" + Environment.NewLine;
            }
            if (model.Email != null && _db.Users.Any(a => a.Email == model.Email))
            {
                errors += "Već postoji korisnik sa unesenom e-mail adresom!" + Environment.NewLine;
            }
            if (model.UserName != null && _db.Users.Any(a => a.UserName == model.UserName))
            {
                errors += "Već postoji korisnik sa unesenim korisničkim imenom!" + Environment.NewLine;
            }
            if (model.Password != null && model.ConfirmPassword != null && model.ConfirmPassword != model.Password)
            {
                errors += "Lozinka i potvrda lozinke se ne poklapaju!" + Environment.NewLine;
            }

            if (model.Password != null)
            {
                var passwordValidator = new PasswordValidator<IdentityUser>();
                var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, null, model.Password);
                errors += AddPasswordErrors(passwordValidationResult);
            }

            var username = User.Identity.Name;
            var _appUser = _db.Users.FirstOrDefault(a => a.UserName == username);

            if (errors != string.Empty) return Json(new BasicDto() { error = errors, info = "" });
            else
            {
                var result = await _userManager.CreateAsync(new ApplicationUser()
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PartnerId = _appUser.PartnerId,
                    LegalEntityId = _appUser.LegalEntityId,
                    CheckInPointId = model.CheckInPointId,
                    Type = (UserType)Enum.Parse(typeof(UserType), model.Type)
                }, model.Password);

                if (result.Succeeded)
                {
                    var user = _db.Users.FirstOrDefault(a => a.UserName == model.UserName);
                    if (user != null)
                    {
                        user.EmailConfirmed = true;
                        _db.SaveChanges();

                        if (user.Type.ToString().StartsWith("Tourist"))
                        {
                            await _userManager.AddToRoleAsync(user, "TouristOrg");
                            await _userManager.AddToRoleAsync(user, user.Type.ToString());
                        }
                    }

                    return Json(new BasicDto() { error = "", info = "Uspješno kreiran korisnički nalog!" });
                }
                else
                {
                }
            }

            return Ok();
        }
    }
}
