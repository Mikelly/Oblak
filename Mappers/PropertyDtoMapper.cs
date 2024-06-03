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
            CreateMap<PropertyEnrichedDto, Property>()
                .ReverseMap()
                .ForMember(a => a.PropertyName, opt => opt.Ignore())
                .ForMember(a => a.PaymentType, opt => opt.Ignore());
                //.ForMember(a => a.LegalEntity, opt => opt.Ignore());
        }
    }
}