using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Computer;

public class CreateComputerViewModel
{ 

    [Required(ErrorMessage = "Naziv je obavezan.")]
    public string PCName { get; set; }

    public string? LocationDescription { get; set; } 
}
