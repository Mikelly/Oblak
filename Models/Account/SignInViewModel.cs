using Oblak.Models.Computer;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Account;

public class SignInViewModel : RegistrationComputerViewModel
{
    [Display(Name = "Username")]
    [Required(ErrorMessage = "Molimo vas unesite e-mail ili korisničko ime")]    
    public string UserName { get; set; }

    [Display(Name = "Lozinka")]
    [Required(ErrorMessage = "Molimo vas unesite lozinku")]
    public string Password { get; set; }

    [Display(Name = "Zapamtite me?")]
    public bool RememberMe { get; set; }
}
