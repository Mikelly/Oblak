using Oblak.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
	public class ResTaxDto
	{
		public int Id { get; set; }

		public int LegalEntityId { get; set; }

		public int PropertyId { get; set; }

		public DateTime Date { get; set; }

		public DateTime DateFrom { get; set; }

		public DateTime DateTo { get; set; }

		public decimal Amount { get; set; }

		public string Status { get; set; }

		public List<ResTaxItemDto> Items { get; set; }

		public static ResTaxDto FromEntity(ResTaxCalc tax)
		{
			var dto = new ResTaxDto();

			dto.Id = tax.Id;
			dto.LegalEntityId = tax.LegalEntityId;
			dto.PropertyId = tax.PropertyId;
			dto.Date = tax.Date;
			dto.DateFrom = tax.DateFrom;
			dto.DateTo = tax.DateTo;
			dto.Amount = tax.Amount;
			dto.Status = tax.Status;
			dto.Items = new List<ResTaxItemDto>();

			foreach (var item in tax.Items)
			{
				var i = new ResTaxItemDto()
				{
					ID = item.ID,
					NumberOfGuests = item.NumberOfGuests,
					NumberOfNights = item.NumberOfNights,
					TaxPerNight = item.TaxPerNight,
					TotalTax = item.TotalTax,
					GuestType = item.GuestType,
					TaxType = item.TaxType
				};

				dto.Items.Add(i);
			}

			return dto;
		}
	}


	public class ResTaxItemDto
	{		
		public int ID { get; set; }
		
		public string TaxType { get; set; }	
		
		public string GuestType { get; set; }
		
		public int NumberOfGuests { get; set; }
		
		public int NumberOfNights { get; set; }
		
		public decimal TaxPerNight { get; set; }
		
		public decimal TotalTax { get; set; }
	}
}
