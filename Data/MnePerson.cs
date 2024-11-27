using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Data
{
    public class MnePerson : Person
    {
        [StringLength(450)]
        public string? BirthPlace { get; set; }

        [StringLength(450)]
        public string BirthCountry { get; set; }

        [StringLength(450)]
        public string Nationality { get; set; }

        [StringLength(450)]
        public string PersonType { get; set; }

        [StringLength(450)]
        public string? PermanentResidenceCountry { get; set; }

        [StringLength(450)]
        public string? PermanentResidencePlace { get; set; }
        
        [StringLength(450)]
        public string? PermanentResidenceAddress { get; set; }
        
        public DateTime CheckIn { get; set; }
        
        public DateTime? CheckOut { get; set; }

        [StringLength(450)]
        public string DocumentType { get; set; }

        [StringLength(450)]
        public string DocumentNumber { get; set; }
        
        public DateTime DocumentValidTo { get; set; }

        [StringLength(450)]
        public string DocumentCountry { get; set; }

        [StringLength(450)]
        public string DocumentIssuer { get; set; }

        [StringLength(450)]
        public string? VisaType { get; set; }

        [StringLength(450)]
        public string? VisaNumber { get; set; }
        
        public DateTime? VisaValidFrom { get; set; }
        
        public DateTime? VisaValidTo { get; set; }

        [StringLength(450)]
        public string? VisaIssuePlace { get; set; }

        [StringLength(450)]
        public string? EntryPoint { get; set; }
        
        public DateTime? EntryPointDate { get; set; }

        [StringLength(450)]
        public string? Other { get; set; }
                
        public int? ResTaxTypeId { get; set; }

        public ResTaxType? ResTaxType { get; set; }

		public int? ResTaxPaymentTypeId { get; set; }

		public ResTaxPaymentType ResTaxPaymentType { get; set; }

        public ResTaxStatus ResTaxStatus { get; set; } = ResTaxStatus.Open;

        public decimal? ResTaxAmount { get; set; }

		public decimal? ResTaxFee { get; set; }
	}
}