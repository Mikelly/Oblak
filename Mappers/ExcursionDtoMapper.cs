using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class ExcursionDtoMapper : Profile
    {
        public ExcursionDtoMapper() {

            CreateMap<Excursion, ExcursionDto>()
                .ForMember(dest => dest.AgencyName, opt => opt.MapFrom(src => src.Agency.Name))
                .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country!.CountryName))
                .ForMember(dest => dest.ExcursionTaxTotal, opt => opt.MapFrom(src => src.NoOfPersons * src.ExcursionTaxAmount));

            CreateMap<ExcursionDto, Excursion>();
        }
    }
}