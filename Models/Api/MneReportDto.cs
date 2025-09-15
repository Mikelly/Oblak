using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api;

public class MneReportDto
{
    [Required(ErrorMessage = "Naziv izvjestaja je obavezan.")]
    public string Report { get; set; } 

    [RegularExpression(@"\d{2}\.\d{2}\.\d{4}", ErrorMessage = "DateFrom mora biti u formatu dd.MM.yyyy")]
    public string DateFrom { get; set; }
     
    [RegularExpression(@"\d{2}\.\d{2}\.\d{4}", ErrorMessage = "DateTo mora biti u formatu dd.MM.yyyy")]
    public string DateTo { get; set; }
     
    public string EnuCode { get; set; } 

}
