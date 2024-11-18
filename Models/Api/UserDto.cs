using Microsoft.AspNetCore.Identity;
using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class UserDto
    {  
        public string? PhoneNumber { get; set; }
        
        public string? Email { get; set; }

        public string? UserName { get; set; }
        
        public string Id { get; set; }

        public int LegalEntityId { get; set; }        

        public int? PartnerId { get; set; }
        
        public string? Language { get; set; }
        
        public string? EfiOperator { get; set; }

        public string? PersonName { get; set; }

        public int? CheckInPointId { get; set; }

        public string Type { get; set; }
    }
}
