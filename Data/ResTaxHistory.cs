using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Data
{
    public class ResTaxHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PersonId { get; set; }

        public DateTime OldCheckIn { get; set; }

        public DateTime OldCheckOut { get; set; }

        public decimal OldResTaxAmount { get; set; }

        [StringLength(450)]
        public string? UserCreated { get; set; }

        public DateTime? UserCreatedDate { get; set; }


        public MnePerson Person { get; set; }
    }
}
