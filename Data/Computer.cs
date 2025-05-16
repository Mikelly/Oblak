using System.ComponentModel.DataAnnotations;

namespace Oblak.Data
{
    public class Computer
    {
        [Key] 
        public Guid Id { get; set; }

        [Required]
        public int PartnerId { get; set; }

        [Required]
        public string PCName { get; set; }

        public string? LocationDescription { get; set; }
        public DateTime? Registered { get; set; }
        public string? UserRegistered { get; set; }
        public DateTime? Logged { get; set; }
        public string? UserLogged { get; set; }
         
        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion

        #region Navigation Properties
        public virtual Partner Partner { get; set; }
        public virtual List<MnePerson> MnePersons { get; set; } 
        public virtual List<ComputerLog> ComputerLogs { get; set; }
        #endregion
    }

}
