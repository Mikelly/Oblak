using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class AgencyDtoMapper : Profile
    {
        public AgencyDtoMapper() {

            CreateMap<Agency, AgencyDto>()
                .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country!.CountryName));

            CreateMap<AgencyDto, Agency>();
        }
    }
}