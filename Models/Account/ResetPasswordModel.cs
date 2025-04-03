using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Account
{
    public class ResetPasswordModel
    {
        [Required]
        public int LegalEntityId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; } 
    }

}
