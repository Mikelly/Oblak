using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data;

public partial class FiscalRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public virtual int Id { get; set; }

    public int LegalEntityId { get; set; }
    
    [StringLength(450)]
    public string BusinessUnitCode { get; set; }

    [StringLength(450)]
    public string FiscalEnuCode { get; set; }

    public DateTime FicalizationDate { get; set; }
    
    public int Invoice { get; set; }
    
    public int InvoiceNo { get; set; }

    [StringLength(450)]
    public FiscalRequestType RequestType { get; set; }
        
    public string? Request { get; set; }    

    public string? Response { get; set; }    

    public decimal Amount { get; set; }

    [StringLength(450)]
    public string? TCR { get; set; }

    [StringLength(450)]
    public string? IIC { get; set; }

    [StringLength(450)]
    public string? FIC { get; set; }

    [StringLength(450)]
    public string? FCDC { get; set; }

    [StringLength(450)]
    public string? Status { get; set; }

    [StringLength(4000)]
    public string? Error { get; set; }    

    public string? UserCreated { get; set; }
    
    public DateTime? UserCreatedDate { get; set; }

    public string? UserModified { get; set; }

    public DateTime? UserModifiedDate { get; set; }

    public LegalEntity LegalEntity { get; set; }
}
