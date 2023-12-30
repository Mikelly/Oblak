using AutoMapper;
using Oblak.Data;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class PropertyDtoMapper : Profile
    {
        public PropertyDtoMapper() {
            CreateMap<Property, PropertyDto>()
                .ReverseMap();
        }
    }

    public class PropertyEnrichedDtoMapper : Profile
    {
        public PropertyEnrichedDtoMapper()
        {
            CreateMap<Property, PropertyEnrichedDto>()
                .ReverseMap();
        }
    }
}