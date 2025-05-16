using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Data
{
    public class ComputerLog
    {
        [Key] 
        public int Id { get; set; }

        [Required]
        public Guid ComputerId { get; set; }
        [Required]
        public DateTime Seen { get; set; } = DateTime.Now;

        public string? Action { get; set; } 
        public string? UsedByUser { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? BrowserName { get; set; }
        public string? OSName { get; set; }
        public string? ScreenResolution { get; set; }
        public string? TimeZone { get; set; }
        public bool IsMobile { get; set; }  

        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion

        #region Navigation Properties 
        public virtual Computer Computer { get; set; }
        #endregion
    }

}
