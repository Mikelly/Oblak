using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oblak.Data
{
    public class LegalEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(450)]
        public LegalEntityType Type { get; set; }

        [StringLength(450)]
        public Country Country { get; set; }

        public int? PartnerId { get; set; }

        public int? AdministratorId { get; set; }

<<<<<<< HEAD
        public int? PassThroughId { get; set; }

=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string TIN { get; set; }

        [StringLength(2000)]
        public string Address { get; set; }

        [StringLength(2000)]
        public string? InvoiceHeader { get; set; }

        public byte[]? Logo { get; set; }    
        
        public bool InVat { get; set; }

		public bool Test { get; set; }


		#region MNE EFI

		public byte[]? EfiCertData { get; set; }

        [StringLength(450)]
        public string? EfiPassword { get; set; }


        #endregion


        #region Rb90 Cert

        public byte[]? Rb90CertData { get; set; }

        [StringLength(450)]
        public string? Rb90Password { get; set; }

        #endregion


        #region Srb UserName & Password

        [StringLength(450)]
        public string? SrbRbUserName { get; set; }

        [StringLength(450)]
        public string? SrbRbPassword { get; set; }

        [StringLength(4000)]
        public string? SrbRbToken { get; set; }

        [StringLength(4000)]
        public string? SrbRbRefreshToken { get; set; }

        #endregion


        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }

        #endregion


        #region Navigation Properties

        public List<Property> Properties { get; set; }
        
        [JsonIgnore]
        public List<Group> Groups { get; set; }
        
        public List<Document> Documents { get; set; }
        
<<<<<<< HEAD
        public List<ResTaxCalc> ResTaxes { get; set; }
=======
        public List<ResTax> ResTaxes { get; set; }
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

        public Partner Partner { get; set; }

		public LegalEntity Administrator { get; set; }

<<<<<<< HEAD
        public LegalEntity PassThrough { get; set; }

        public List<LegalEntity> Clients { get; set; }
=======
		public List<LegalEntity> Clients { get; set; }
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

		#endregion
	}
}