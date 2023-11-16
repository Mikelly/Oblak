using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class DocumentPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 
        public int DocumentId { get; set; }
        public Document Document { get; set; }
        public PaymentType PaymentType { get; set; } = PaymentType.Cash;
        public decimal Amount { get; set; }
    }
}
