using Oblak.Data;
using Oblak.Models.Api;

namespace Oblak.Interfaces;

public interface IRegisterBase
{
    public Task<Group> Group(GroupDto group);

    public Task<BasicDto> GroupDelete(int id);

}
