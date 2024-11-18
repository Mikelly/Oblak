using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class CountryDto
    {
        public int Id { get; set; }
                
        public string CountryCode2 { get; set; }
                
        public string CountryCode3 { get; set; }
                
        public string CountryName { get; set; }
    }
}
