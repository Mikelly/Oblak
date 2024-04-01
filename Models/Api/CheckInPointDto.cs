using Oblak.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class CheckInPointDto
    {
        public int Id { get; set; }

        public int PartnerId { get; set; }
        
        public string Name { get; set; }
        
        public string Address { get; set; }        
        
        public string? Location { get; set; }

        public string? Status { get; set; }
    }
}