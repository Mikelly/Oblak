using AutoMapper;
using Oblak.Data;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class PropertyDtoMapper : Profile
    {
        public PropertyDtoMapper() {
            CreateMap<Property, PropertyDto>()
                .ReverseMap()
                .ForMember(a => a.PropertyName, opt => opt.Ignore())
                .ForMember(a => a.PaymentType, opt => opt.Ignore())
                .ForMember(a => a.LegalEntity, opt => opt.Ignore())
                .ForMember(a => a.Groups, opt => opt.Ignore())
                .ForMember(a => a.Persons, opt => opt.Ignore())
                .ForMember(a => a.GuestTokens, opt => opt.Ignore())
                .ForMember(a => a.ResTaxes, opt => opt.Ignore())
                .ForMember(a => a.Municipality, opt => opt.Ignore())
                ;
        }
    }

    public class PropertyEnrichedDtoMapper : Profile
    {
        public PropertyEnrichedDtoMapper()
        {
            CreateMap<Property, PropertyEnrichedDto>()
                .ReverseMap()
                .ForMember(a => a.PropertyName, opt => opt.Ignore())
                .ForMember(a => a.PaymentType, opt => opt.Ignore())
                .ForMember(a => a.LegalEntity, opt => opt.Ignore())
                .ForMember(a => a.Groups, opt => opt.Ignore())
                .ForMember(a => a.Persons, opt => opt.Ignore())
                .ForMember(a => a.GuestTokens, opt => opt.Ignore())
                .ForMember(a => a.ResTaxes, opt => opt.Ignore())
                .ForMember(a => a.Municipality, opt => opt.Ignore())
                ;
        }
    }
}