using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Agency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PartnerId { get; set; }

        public int CountryId { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string? TIN { get; set; }

		[StringLength(450)]
		public string? TAX { get; set; }

		[StringLength(450)]
		public string? Address { get; set; }

		[StringLength(450)]
		public string? Email { get; set; }

		[StringLength(450)]
		public string? PhoneNo { get; set; }

		[StringLength(450)]
		public string? ContactPerson { get; set; }

        public int DueDays { get; set; } = 0;

        public bool HasContract { get; set; } = false;


        #region Audid Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]
        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }

		#endregion


		public Partner Partner { get; set; }

        public Country Country { get; set; }

        public List<ExcursionInvoice> ExcursionInvoices { get; set; }
    }
}
