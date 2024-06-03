using DocumentFormat.OpenXml.Wordprocessing;
using Oblak.Data;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class MneGuestListDto
    {
        public int ID { get; set; } 
		public string FirstName { get; set; } 
		public string LastName { get; set; } 
		public string PersonalNumber { get; set; } 
		public DateTime BirthDate { get; set; }
		public string DocumentType { get; set; } 
		public string DocumentNumber { get; set; } 
		public string DocumentCountry { get; set; }
		[UIHint("DateTime")]
		public DateTime CheckIn { get; set; }
        [UIHint("DateTime")]
        public DateTime? CheckOut { get; set; } 
		public decimal? ResTaxAmount { get; set; }
    }
}
