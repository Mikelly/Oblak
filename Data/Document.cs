using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Document
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? DocumentId { get; set; }

        [StringLength(450)]
        public string DocumentType { get; set; }

        [StringLength(450)]
        public string IdEncrypted { get; set; }
        
        public int LegalEntityId { get; set; }

        public int? GroupId { get; set; }              

        public int PropertyId { get; set; }                
        
        [StringLength(450)]
        public string InvoiceType { get; set; }
        
        [StringLength(450)]
        public string TypeOfInvoce { get; set; }
        
        public int BusinessUnitId { get; set; }               
        
        [StringLength(450)]
        public string BusinessUnitCode { get; set; }
        
        //public int FiscalEnuId { get; set; }
        
        //public FiscalEnu FiscalEnu { get; set; }
        
        [StringLength(450)]
        public string FiscalEnuCode { get; set; }
        
        public DateTime InvoiceDate { get; set; }
        
        public DateTime? FiscalizationDate { get; set;}
        
        public int No { get; set; }

        public int OrdinalNo { get; set; }

        [StringLength(450)]        
        public string ExternalNo { get; set; }
        
        [StringLength(450)]
        public string FiscalNo { get; set; }
        
        [StringLength(450)]
        public string IIC { get; set; }
        
        [StringLength(450)]
        public string FIC { get; set; }
        
        [StringLength(450)]
        public string OperatorCode { get; set; }
        
        public int? PartnerId { get; set; }
        
        [StringLength(450)]
        public string PartnerName { get; set; }
        
        [StringLength(450)]        
        public string PartnerType { get; set; }
        
        [StringLength(450)]        
        public string PartnerIdType { get; set; }
        
        [StringLength(450)]        
        public string PartnerIdNumber { get; set; }
        
        [StringLength(450)]
        public string PartnerAddress { get; set; }
        
        public decimal Amount { get; set; }

        [StringLength(450)]
        public string CurrencyCode { get; set; } = "EUR";

        [StringLength(450)]
        public string Status { get; set; } = "A";
        
        [StringLength(450)]
        public string Qr { get; set; }

        [StringLength(4000)]
        public string QrPath { get; set; }
        
        public decimal ExchangeRate { get; set; }



        public Property Property { get; set; }

        public Group? Group { get; set; }

        public LegalEntity LegalEntity { get; set; }

        public List<DocumentItem> DocumentItems { get; set; }
        
        public List<DocumentPayment> DocumentPayments { get; set; }
    }
}
