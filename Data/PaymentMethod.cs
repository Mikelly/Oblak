using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class PaymentMethod
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? PaymentTransactionId { get; set; }
        public PaymentTransaction PaymentTransaction { get; set; }
        public int? LegalEntityId { get; set; }
        public LegalEntity LegalEntity { get; set; }
        public string Type { get; set; }
        public string LastFourDigits { get; set; }
        public string UserCreated { get; set; }
        public DateTime UserCreatedDate { get; set; }
        [StringLength(450)]
        public string? UserModified { get; set; }
        public DateTime? UserModifiedDate { get; set; }
    }
}
