using AutoMapper;
using Oblak.Data;
using Oblak.Models.Computer;

namespace Oblak.Mappers;

public class ComputerDtoMapper : Profile
{
    public ComputerDtoMapper()
    {
        CreateMap<Computer, ComputerDto>();

        CreateMap<ComputerDto, Computer>()
            .ForMember(dest => dest.PartnerId, opt => opt.Ignore())
            .ForMember(dest => dest.Partner, opt => opt.Ignore()); 
    }
}
