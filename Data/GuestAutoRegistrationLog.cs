using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class GuestAutoRegistrationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MnePersonId { get; set; }

        public int? ExternalId { get; set; }
        
        public int LegalEntityId { get; set; }
        
        public int PropertyId { get; set; }
        
        public int? GroupId { get; set; }
        
        public DateTime? CheckOut { get; set; }

        public bool Success { get; set; }

        [StringLength(450)]
        public string? InitializedBy { get; set; }

        [StringLength(4000)]
        public string? ValidationErrors { get; set; }

        [StringLength(4000)]
        public string? ExternalErrors { get; set; }

        public DateTime CreatedDate { get; set; }

        public MnePerson MnePerson { get; set; }
        
        public LegalEntity LegalEntity { get; set; }
        
        public Property Property { get; set; }
        
        public Group? Group { get; set; }
    }
}
