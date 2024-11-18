using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class CountryDtoMapper : Profile
    {
        public CountryDtoMapper() {
            CreateMap<Country, CountryDto>().ReverseMap();
        }
    }
}