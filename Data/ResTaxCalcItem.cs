using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ResTaxCalcItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int ResTaxID { get; set; }

        public ResTaxCalc ResTax { get; set; }

        [StringLength(450)]
        public string TaxType { get; set; }

        [StringLength(450)]
        public string GuestType { get; set; }

        public int NumberOfGuests { get; set; }

        public int NumberOfNights { get; set; }

        public decimal TaxPerNight { get; set; }

        public decimal TotalTax { get; set; }

		[StringLength(450)]
		public string? UserCreated { get; set; }

		public DateTime? UserCreatedDate { get; set; }

		[StringLength(450)]
		public string? UserModified { get; set; }

		public DateTime? UserModifiedDate { get; set; }
	}
}
