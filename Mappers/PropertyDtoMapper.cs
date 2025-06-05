using AutoMapper;
using Oblak.Data;
using Oblak.Models.Api;

namespace Oblak.Mappers;

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
            .ForMember(dest => dest.PropertyName, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentType, opt => opt.Ignore())
            .ForMember(dest => dest.LegalEntity, opt => opt.Ignore())
            .ForMember(dest => dest.Groups, opt => opt.Ignore())
            .ForMember(dest => dest.Persons, opt => opt.Ignore())
            .ForMember(dest => dest.GuestTokens, opt => opt.Ignore())
            .ForMember(dest => dest.ResTaxes, opt => opt.Ignore())
            .ForMember(dest => dest.Municipality, opt => opt.Ignore());
         
        CreateMap<PropertyEnrichedDto, Property>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RegNumber, opt => opt.MapFrom(src => src.RegNumber))
            .ForMember(dest => dest.RegDate, opt => opt.MapFrom(src => src.RegDate))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.MunicipalityId, opt => opt.MapFrom(src => src.MunicipalityId))
            .ForMember(dest => dest.Place, opt => opt.MapFrom(src => src.Place))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForAllMembers(opt => opt.Ignore());
    }
}