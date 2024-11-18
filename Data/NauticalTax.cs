using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class NauticalTax
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int PartnerId { get; set; }

        public Partner Partner { get; set; }

		[StringLength(450)]
		public VesselType VesselType { get; set; }

		public decimal Amount { get; set; }		

		public decimal LowerLimitLength { get; set; }

		public decimal UpperLimitLength {  get; set; }

		public int LowerLimitPeriod { get; set; }

		public int UpperLimitPeriod { get; set; }



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