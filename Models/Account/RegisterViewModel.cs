using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Account
{
    public class RegisterViewModel
    {
        [EmailAddress]
        [Required(ErrorMessage = "Morate unijeti e-mail adresu")]
        public string Email { get; set; }
        [Phone]
        public string? PhoneNumber { get; set; }
        
        [Required]        
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [Compare("Password", ErrorMessage = "Lozinka i potvrda lozinke nisu isti.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Morate unijeti državu korisnika")]
        public Country Country { get; set; }

        [Required(ErrorMessage = "Morate odabrati da li je korisnik pravno ili fizičko lice")]
        public string LegalEntityType { get; set; }

        [Required(ErrorMessage = "Morate unijeti naziv pravnog subjekta korisnika")]
        public string LegalEntityName { get; set; }

        [Required(ErrorMessage = "Morate unijeti poreski broj pravnog subjekta korisnika")]
        public string LegalEntityTIN { get; set; }

        //[Required(ErrorMessage = "Morate unijeti adresu pravnog subjekta korisnika")]
        public string? LegalEntityAddress { get; set; }

        [Required(ErrorMessage = "Morate unijeti naziv pravnog subjekta korisnika")]
        public bool LegalEntityInVat { get; set; }

        public string? Reference { get; set; }
<<<<<<< HEAD
    }

    public class AccountViewModel
    {
        [EmailAddress]
        public string Email { get; set; }
        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string? Type { get; set; }

        public int? CheckInPointId { get; set; }
=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
    }
}
