namespace Oblak.Models.Api;

public class PrevStayDto
{
    public int PersonId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
    public string DocumentNumber { get; set; }

    public string PropertyName { get; set; }
    public string? PropertyAddress { get; set; }

    public string LegalEntityName { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public DateTime? EntryPointDate { get; set; }

    public string? EntryPoint { get; set; }
    
}
