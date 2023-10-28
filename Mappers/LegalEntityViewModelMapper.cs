using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.rb90;

namespace Oblak.Mappers
{
    public class LegalEntityMapper : Profile
    {
        public LegalEntityMapper() {
            CreateMap<LegalEntity, LegalEntityViewModel>().ReverseMap();
        }
    }
}