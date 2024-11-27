using Oblak.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api;

public class ResTaxFeeDto
{
	public int Id { get; set; }

	public int PartnerId { get; set; }

	public Partner Partner { get; set; }

	public string Description { get; set; }

	public int ResTaxPaymentTypeId { get; set; }

	public string ResTaxPaymentType { get; set; }

	public decimal? FeeAmount { get; set; }

	public decimal? FeePercentage { get; set; }

	public decimal LowerLimit { get; set; }

	public decimal UpperLimit { get; set; }

	public string? UserCreated { get; set; }

	public DateTime? UserCreatedDate { get; set; }

	public string? UserModified { get; set; }

	public DateTime? UserModifiedDate { get; set; }
}
