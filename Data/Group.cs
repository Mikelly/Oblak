using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Group
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int LegalEntityId { get; set; }

        public int PropertyExternalId { get; set; }
        
        public int PropertyId { get; set; }                
        
        public Nullable<int> UnitId { get; set; }
        
        [StringLength(36)]
        public string Guid { get; set; }
        
        public DateTime Date { get; set; }

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        [StringLength(450)]
        public string? Email { get; set; }
        
        [StringLength(450)]
        public string? Phone { get; set; }
        
        
        [StringLength(450)]
        public string? Description { get; set; }
        
        [StringLength(2000)]
        public string? Note { get; set; }

        [StringLength(450)]
        public string Status { get; set; } = "A";

        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]
        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }


        #region Navigation Properties

        public LegalEntity LegalEntity { get; set; }

        public Property Property { get; set; }

        public List<Person> Persons { get; set; }

        #endregion
    }
}
