using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
    public class Vessel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                
        
        public int PartnerId { get; set; }

		public int? LegalEntityId { get; set; }

        [StringLength(450)]
		public VesselType VesselType { get; set; }

		[StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string Registration { get; set; }
                
		public int? CountryId { get; set; }

		public int Length { get; set; }

        [StringLength(450)]
        public string OwnerName { get; set; }

        [StringLength(450)]
        public string? OwnerAddress { get; set; }

        [StringLength(450)]
        public string? OwnerTIN { get; set; }

        [StringLength(450)]
        public string? OwnerPhone { get; set; }

        [StringLength(450)]
        public string? OwnerEmail { get; set; }


        public Partner Partner { get; set; }

        public Country Country { get; set; }

        public LegalEntity? LegalEntity { get; set; }



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