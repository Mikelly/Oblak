using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
    public class MneMupData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                
        
        public int PartnerId { get; set; }

		[StringLength(450)]
		public string LegalEntityName { get; set; }

		[StringLength(450)]
		public string LegalEntityCode { get; set; }

		[StringLength(450)]
        public string Address { get; set; }

        [StringLength(450)]
        public string TIN { get; set; }
                
		public DateTime DateOfBirth { get; set; }

		public DateTime CheckIn { get; set; }

		public DateTime CheckOut { get; set; }

        [StringLength(450)]
        public string Gender { get; set; }

        [StringLength(450)]
        public string? DocumentCountry { get; set; }


		#region Audit Properties

		[StringLength(450)]
        public string? UserCreated { get; set; }        
        
        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]        
        public string? UserModified { get; set; }
        
        public DateTime? UserModifiedDate { get; set; }

        #endregion
    }
}