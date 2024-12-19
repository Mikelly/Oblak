using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Data
{
    public class ResTaxHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? PersonId { get; set; }

        public DateTime PrevCheckIn { get; set; }

        public DateTime? PrevCheckOut { get; set; }

        public decimal? PrevResTaxAmount { get; set; }

        public decimal? PrevResFeeAmount { get; set; }

        public int? PrevResTaxTypeId { get; set; }

        public int? PrevResTaxPaymentTypeId { get; set; }

        public int? PrevResTaxExemptionTypeId { get; set; }


        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }


        public MnePerson Person { get; set; }
    }
}
