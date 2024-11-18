using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
    public abstract class Person
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(450)]
        public string? Guid { get; set; }
                
        public int? ExternalId { get; set; }
        
        public int LegalEntityId { get; set; }
        
        public int PropertyExternalId { get; set; }
        
        public int PropertyId { get; set; }

        public int? CheckInPointId { get; set; }

        public int? GroupId { get; set; }

        [StringLength(450)]
        public string LastName { get; set; }
        
        [StringLength(450)]        
        public string FirstName { get; set; }
        
        [StringLength(450)]
        public string? PersonalNumber { get; set; }
        
        [StringLength(450)]
        public string Gender { get; set; }
        
        public DateTime BirthDate { get; set; }        

        public bool IsDeleted { get; set; }
        
        [StringLength(450)]
        public string? Status { get; set; }
        
        [StringLength(4000)]
        public string? Error { get; set; }

		[StringLength(4000)]
		public string? Note { get; set; }

		[StringLength(450)]
        public string? UserCreated { get; set; }
        
        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]
        public string? UserModified { get; set; }

        [StringLength(450)]
        public DateTime? UserModifiedDate { get; set; }

        public LegalEntity LegalEntity { get; set; }
        
        public Property Property { get; set; }
        
        public Group? Group { get; set; }

        public CheckInPoint? CheckInPoint { get; set; }
    }
}