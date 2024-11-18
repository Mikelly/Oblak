using System;

namespace Oblak.Models.Api
{
    public class AgencyDto
    {
        public int Id { get; set; }

        public int PartnerId { get; set; }

        public int CountryId { get; set; }

        public string CountryName { get; set; }

        public string Name { get; set; }

        public string TIN { get; set; }

        public string? TAX { get; set; }

        public string Address { get; set; }

        public string Email { get; set; }

        public string? PhoneNo { get; set; }

        public string? ContactPerson { get; set; }

        public int DueDays { get; set; }

        public bool HasContract { get; set; }

        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }

        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }
    }
}
