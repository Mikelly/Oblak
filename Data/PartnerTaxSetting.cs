using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class PartnerTaxSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PartnerId { get; set; }

        [StringLength(450)]
        public TaxType TaxType { get; set; }

        public bool UseAdvancePayment { get; set; } = false;

        [StringLength(450)]
        public string PaymentDescription { get; set; }

        [StringLength(450)]
        public string PaymentAccount { get; set; }

        [StringLength(450)]
        public string PaymentAddress { get; set; }

        [StringLength(450)]
        public string PaymentName { get; set; } = "";

        [StringLength(450)]
        public string Code { get; set; } = "030";

        [StringLength(450)]
        public string Model { get; set; } = "18";

        public decimal TaxPrice { get; set; }       


        #region Audid Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }

        [StringLength(450)]
        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }

		#endregion


		public Partner Partner { get; set; }
    }
}
