using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Account
{
    public class LockUnlockModel
    {
        [Required] 
        public string EntityId { get; set; } 

        [Required] 
        public bool? Lock { get; set; }
    }

}
