using Oblak.Data.Enums;
using System.Collections.Generic;

namespace Oblak.Data.Api
{
    public class LegalEntityDto
    {
        public int Id { get; set; }

        public LegalEntityType Type { get; set; }

        public string Country { get; set; }

        public int? PartnerId { get; set; }

        public int? AdministratorId { get; set; }

        public int? PassThroughId { get; set; }

        public string Name { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? DocumentType { get; set; }

        public string? DocumentNumber { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string TIN { get; set; }

        public string Address { get; set; }

        public string? InvoiceHeader { get; set; }
        
        public bool InVat { get; set; }

        public bool IsRegistered { get; set; }

        public bool Test { get; set; }

        public string? PaytenUserId { get; set; }

        public string? EfiPassword { get; set; }

        public string? Rb90Password { get; set; }

        public string? SrbRbUserName { get; set; }

        public string? SrbRbPassword { get; set; }

        public string? SrbRbToken { get; set; }

        public string? SrbRbRefreshToken { get; set; }
    }
}
