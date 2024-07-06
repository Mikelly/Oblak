using Oblak.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oblak.Data
{
    public class Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? PartnerId { get; set; }

        public int? LegalEntityId { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string Action { get; set; }

		[StringLength(450)]
		public string UserName { get; set; }

        public string? Data { get; set; }
	}
}
