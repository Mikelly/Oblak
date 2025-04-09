using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Account
{
    public class LockUnlockModel
    {
        [Required] 
        public int LegalEntityId { get; set; } 

        [Required] 
        public bool? Lock { get; set; }
    }

}
