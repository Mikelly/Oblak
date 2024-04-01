using Oblak.Data.Enums;
<<<<<<< HEAD
using System.ComponentModel.DataAnnotations;
=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

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

        [Display(Name = "Poreski broj")]
        public string TIN { get; set; }

        [UIHint("Editor")]
        [Display(Name = "Adresa")]
        public string Address { get; set; }

        [Display(Name = "Država")]
        [ScaffoldColumn(false)]
        public string Country { get; set; }

        public bool InVat { get; set; }

        [ScaffoldColumn(false)]
        public string? UserCreated { get; set; }

        [ScaffoldColumn(false)]
        public DateTime? UserCreatedDate { get; set; }

        [ScaffoldColumn(false)]
        public string? UserModified { get; set; }

        [ScaffoldColumn(false)]
        public DateTime? UserModifiedDate { get; set; }
    }
}
