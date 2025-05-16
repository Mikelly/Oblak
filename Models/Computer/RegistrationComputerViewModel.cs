using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Computer
{
    public class RegistrationComputerViewModel
    {
        public Guid? Id { get; set; }
          
        public string? UserAgent { get; set; }

        public string? BrowserName { get; set; }

        public string? OSName { get; set; }

        public string? ScreenResolution { get; set; }

        public string? TimeZone { get; set; }

        public bool IsMobile { get; set; }
        public string? PIN { get; set; }
    }
}
