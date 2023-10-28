using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data;

public class SelfRegisterToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }    
    
    public int LegalEntityId { get; set; }       

    [StringLength(36)]
    public string Guid { get; set; }
    
    [StringLength(450)]
    public string Email { get; set; }
    
    [StringLength(450)]
    public string PhoneNo { get; set; }
    
    public int? PropertyId { get; set; }
    
    public int? PropertyUnitId { get; set; }
    
    public DateTime? Sent { get; set; }
    
    public DateTime? Expires { get; set; }

    [StringLength(450)]
    public string Status { get; set; } = "A";

    [StringLength(450)]
    public string? UserCreated { get; set; }
    
    public DateTime? UserCreatedDate { get; set; }


    public LegalEntity LegalEntity { get; set; }

    public Property? Property { get; set; }
}
