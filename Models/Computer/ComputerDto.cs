using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Computer;

public class ComputerDto
{ 
    public Guid? Id { get; set; }

    [Required]
    public string PCName { get; set; }
    public string? LocationDescription { get; set; } 
    public DateTime? Registered { get; set; }
    public string? UserRegistered { get; set; }
    public DateTime? Logged { get; set; }
    public string? UserLogged { get; set; }


    [StringLength(450)]
    public string? UserCreated { get; set; }
    public DateTime? UserCreatedDate { get; set; }
    [StringLength(450)]
    public string? UserModified { get; set; }
    public DateTime? UserModifiedDate { get; set; }
     
}
