using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class PartnerDto
    {        
        public int Id { get; set; }
             
        public Country Country { get; set; }
                
        public string Name { get; set; }
                
        public string? TIN { get; set; }
                
        public PartnerType? PartnerType { get; set; }
                
        public string? Address { get; set; }

        public string? Reference { get; set; }
    }
}
