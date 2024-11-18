using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class NauticalTaxDtoMapper : Profile
    {
        public NauticalTaxDtoMapper() {
            CreateMap<NauticalTax, NauticalTaxDto>().ReverseMap();
        }
    }
}