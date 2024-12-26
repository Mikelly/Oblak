using System.ComponentModel.DataAnnotations;

namespace Oblak.Models
{
    public class LegalEntityViewModel
    {        
        public int Id { get; set; }

        [UIHint("GridForeignKey")]
        public string Type { get; set; }

        [UIHint("Integer")]
        public int? AdministratorId { get; set; }

        [Display(Name = "Naziv")]
        public string Name { get; set; }

        [Display(Name = "Ime")]
        public string FirstName { get; set; }

        [Display(Name = "Prezime")]
        public string LastName { get; set; }

        [Display(Name = "Poreski broj")]
        public string TIN { get; set; }

        [Display(Name = "Broj telefona")]
        public string PhoneNumber { get; set; }

        [UIHint("Editor")]
        [Display(Name = "Adresa")]
        public string Address { get; set; }

        [Display(Name = "Država")]
        [ScaffoldColumn(false)]
        public string Country { get; set; }

        public bool InVat { get; set; }

        public bool IsRegistered { get; set; }

        public string? Email { get; set; }

        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }
    }
}
