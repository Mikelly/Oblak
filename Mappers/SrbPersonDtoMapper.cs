using AutoMapper;
using Oblak.Data;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class SrbPersonDtoMapper : Profile
    {
        public SrbPersonDtoMapper() {
            CreateMap<SrbPerson, SrbPersonDto>()
                .ReverseMap();
        }
    }

    public class SrbPersonEnrichedDtoMapper : Profile
    {
        public SrbPersonEnrichedDtoMapper()
        {
            CreateMap<SrbPerson, SrbPersonEnrichedDto>()
                .ReverseMap();
        }
    }
}