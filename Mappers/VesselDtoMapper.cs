using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class VesselDtoMapper : Profile
    {
        public VesselDtoMapper() {
            CreateMap<Vessel, VesselDto>()
                .ForMember(dest => dest.LegalEntityName, opt => opt.MapFrom(src => src.LegalEntity!.Name))
                .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country!.CountryName))
                ;

            CreateMap<VesselDto, Vessel>();
        }
    }
}