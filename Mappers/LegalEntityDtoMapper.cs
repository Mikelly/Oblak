using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;

namespace Oblak.Mappers
{
    public class LegalEntityDtoMapper : Profile
    {
        public LegalEntityDtoMapper() {
            CreateMap<LegalEntity, LegalEntityDto>().ReverseMap();
        }
    }
}