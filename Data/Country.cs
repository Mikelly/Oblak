using System.ComponentModel.DataAnnotations;

namespace Oblak.Data
{
    public class Country
    {
        public int Id { get; set; }

        [StringLength(16)]
        public string CountryCode2 { get; set; }

        [StringLength(16)]
        public string CountryCode3 { get; set; }

        [StringLength(450)]
        public string CountryName { get; set; }
    }
}
