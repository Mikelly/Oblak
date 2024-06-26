﻿using Oblak.Data.Enums;
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
        public DocumentType DocumentType { get; set; } = DocumentType.Invoice;

        [StringLength(450)]
        public string IdEncrypted { get; set; } = string.Empty;
        
        public int LegalEntityId { get; set; }

        public int? GroupId { get; set; }              

        public int PropertyId { get; set; }

        [StringLength(450)]
        public InvoiceType InvoiceType { get; set; } = InvoiceType.Invoice;

        [StringLength(450)]
        public TypeOfInvoice TypeOfInvoce { get; set; } = TypeOfInvoice.Cash;
        
        [StringLength(450)]
        public string BusinessUnitCode { get; set; }
        
        [StringLength(450)]
        public string FiscalEnuCode { get; set; }
        
        public DateTime InvoiceDate { get; set; }
        
        public DateTime? FiscalizationDate { get; set;}
        
        public int No { get; set; }

        public int OrdinalNo { get; set; }

        [StringLength(450)]
        public string ExternalNo { get; set; } = string.Empty;
        
        [StringLength(450)]
        public string? FiscalNo { get; set; }
        
        [StringLength(450)]
        public string? IIC { get; set; }
        
        [StringLength(450)]
        public string? FIC { get; set; }
        
        [StringLength(450)]
        public string OperatorCode { get; set; }
        
        public int? PartnerId { get; set; }

        [StringLength(450)]
        public string PartnerName { get; set; } = string.Empty;

        [StringLength(450)]
        public BuyerType PartnerType { get; set; } = BuyerType.Person;

        [StringLength(450)]
        public BuyerIdType PartnerIdType { get; set; } = BuyerIdType.Passport;

        [StringLength(450)]
        public string PartnerIdNumber { get; set; } = "00000000";
        
        [StringLength(450)]
        public string? PartnerAddress { get; set; }
        
        public decimal Amount { get; set; } = decimal.Zero;

        [StringLength(450)]
        public string CurrencyCode { get; set; } = "EUR";

        [StringLength(450)]
        public DocumentStatus Status { get; set; } = DocumentStatus.Active;
        
        [StringLength(450)]
        public string? Qr { get; set; }

        [StringLength(4000)]
        public string? QrPath { get; set; }
        
        public decimal ExchangeRate { get; set; } = decimal.One;

        public Guid PaytenOrderId { get; set; } = Guid.NewGuid();

        #region Audit Properties

        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }
        [StringLength(450)]

        public string? UserModified { get; set; }

        public DateTime? UserModifiedDate { get; set; }

        #endregion


        public Property Property { get; set; }

        public Group? Group { get; set; }

        public LegalEntity LegalEntity { get; set; }

        public List<DocumentItem> DocumentItems { get; set; } = new();
        
        public List<DocumentPayment> DocumentPayments { get; set; } = new();
    }
}
