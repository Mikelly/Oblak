using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Oblak.Data.Enums;

namespace Oblak.Data
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(450)]
        public UserType Type { get; set; }

        public int LegalEntityId { get; set; }

        public int? PartnerId { get; set; }

        [StringLength(450)]
        public string? Language { get; set; }

        [StringLength(450)]
        public string? EfiOperator { get; set; }

        [StringLength(450)]
        public string? PersonName { get; set; }

        public int? CheckInPointId { get; set; }

        public bool IsCertRequired { get; set; } = false;


        #region Audit Properties

        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion


        #region Navigation Properties

        public LegalEntity LegalEntity { get; set; }

        #endregion
    }
}