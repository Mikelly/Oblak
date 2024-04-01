using AutoMapper;
using Oblak.Data;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class SrbPersonDtoMapper : Profile
    {
        public SrbPersonDtoMapper() {
            CreateMap<SrbPerson, SrbPersonDto>()
<<<<<<< HEAD
=======
                .ReverseMap();
        }
    }

    public class SrbPersonEnrichedDtoMapper : Profile
    {
        public SrbPersonEnrichedDtoMapper()
        {
            CreateMap<SrbPerson, SrbPersonEnrichedDto>()
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
                .ReverseMap();
        }
    }

    public class SrbPersonEnrichedDtoMapper : Profile
    {
        public SrbPersonEnrichedDtoMapper()
        {
            CreateMap<SrbPerson, SrbPersonEnrichedDto>();
            CreateMap<SrbPersonEnrichedDto, SrbPerson>()            
                .ForMember(a => a.LegalEntityId, opt => opt.Ignore())                
                .ForMember(a => a.LegalEntity, opt => opt.Ignore());
                
        }
    }
}