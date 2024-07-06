using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class PaymentTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? DocumentId { get; set; }
        public Document Document { get; set; }
        public int? GroupId { get; set; }
        public Group Group { get; set; }
        public int? LegalEntityId { get; set; }
        public LegalEntity LegalEntity { get; set; }
        public int? PropertyId { get; set; }
        public Property Property { get; set; }
        [StringLength(450)]
        public string? Token { get; set; }
        [StringLength(450)]
        public string Type { get; set; }
        public decimal Amount { get; set; } = decimal.Zero;
        public decimal SurchargeAmount { get; set; } = decimal.Zero;
        [StringLength(450)]
        public string? Status { get; set; }
        public bool? Success { get; set; }
        public bool? WithRegister { get; set; }
        public string? MerchantTransactionId { get; set; }
        public string? ReferenceUuid { get; set; }
        [Column(TypeName = "NVARCHAR(MAX)")]
        public string? ResponseJson { get; set; }
        [StringLength(450)]
        public string UserCreated { get; set; }
        public DateTime UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
    }
}
