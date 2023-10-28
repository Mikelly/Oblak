using Oblak.Data;
using Oblak.Models.Api;

namespace Oblak.Interfaces;

public interface IRegister
{
    public Task<Person> Person(object person);

    public Task<Dictionary<string, List<string>>> RegisterGroup(Group group, DateTime? checkInDate, DateTime? checkOutDate);

    public Task<Dictionary<string, List<string>>> RegisterPerson(Person person, DateTime? checkInDate, DateTime? checkOutDate);

    public Dictionary<string, List<string>> Validate(Group group, DateTime? checkInDate, DateTime? checkOutDate);

    public Task<List<CodeList>> CodeLists();

    public Task<object> Authenticate();

    public Task CertificateMail(Group group, string email);

    public Task<Stream> CertificatePdf(Group group);

    public Task SendGuestToken(int propertyId, int? unitId, string email, string phoneNo, string lang);
}
