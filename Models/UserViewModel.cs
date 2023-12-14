namespace Oblak.Models
{
    public class UserViewModel
    {        
        public string Id { get; set; }

        public string LegalEntity { get; set; }

        public string Type { get; set; }
                
        public string UserName { get; set; }
                
        public string Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? EfiOperator { get; set; }
        
        public string? UserCreated { get; set; }
        
        public DateTime? UserCreatedDate { get; set; }
        
        public string? UserModified { get; set; }
        
        public DateTime? UserModifiedDate { get; set; }
    }
}
