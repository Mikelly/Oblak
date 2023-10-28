using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class ResTaxItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int ResTaxID { get; set; }
        public ResTax ResTax { get; set; }
        [StringLength(450)]
        public string TaxType { get; set; }
        [StringLength(450)]
        public string GuestType { get; set; }
        public int NumberOfGuests { get; set; }
        public int NumberOfNights { get; set; }
        public decimal TaxPerNight { get; set; }
        public decimal TotalTax { get; set; }
    }
}
