using AutoMapper;
using Oblak.Data;
using Oblak.Models;

namespace Oblak.Mappers
{
    public class LegalEntityMapper : Profile
    {
        public LegalEntityMapper() {
            CreateMap<LegalEntity, LegalEntityViewModel>().ReverseMap();
        }
    }
}