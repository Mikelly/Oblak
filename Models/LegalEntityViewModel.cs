using Oblak.Data.Enums;
using Oblak.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models
{
    public class LegalEntityViewModel
    {        
        public int Id { get; set; }
             
        public LegalEntityType Type { get; set; }
                
        public string Name { get; set; }
                
        public string TIN { get; set; }

        public string Address { get; set; }
                
        public string Country { get; set; }

        public bool InVat { get; set; }        
        
        public string? UserCreated { get; set; }
        
        public DateTime? UserCreatedDate { get; set; }
        
        public string? UserModified { get; set; }
        
        public DateTime? UserModifiedDate { get; set; }
    }
}
