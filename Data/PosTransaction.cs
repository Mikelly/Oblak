using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class PosTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public Document Document { get; set; }
        public int? LegalEntityId { get; set; }
        public LegalEntity LegalEntity { get; set; }
        public int? PropertyId { get; set; }
        public Property Property { get; set; }
        [StringLength(450)]
        public string PaymentSessionToken { get; set; }
        [StringLength(450)]
        public string TransactionType { get; set; }
        public decimal Amount { get; set; } = decimal.Zero;
        [StringLength(450)]
        public string? Status { get; set; }
        public bool? Success { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        [StringLength(450)]
        public string UserCreated { get; set; }
        public DateTime UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
    }
}
