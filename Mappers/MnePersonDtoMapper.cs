using AutoMapper;
using Oblak.Data;
using Oblak.Models.Api;
using Oblak.Models.rb90;

namespace Oblak.Mappers
{
    public class MnePersonDtoMapper : Profile
    {
        public MnePersonDtoMapper() {
            CreateMap<MnePerson, MnePersonDto>()
                .ReverseMap();
        }
    }

    public class MnePersonEnrichedDtoMapper : Profile
    {
        public MnePersonEnrichedDtoMapper()
        {
            CreateMap<MnePerson, MnePersonEnrichedDto>()
                .ReverseMap();
        }
    }
}