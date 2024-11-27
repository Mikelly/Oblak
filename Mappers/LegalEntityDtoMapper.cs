using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;

namespace Oblak.Mappers
{
    public class LegalEntityDtoMapper : Profile
    {
        public LegalEntityDtoMapper() {
            CreateMap<LegalEntity, LegalEntityDto>();
                //.ForMember(dest => dest.IsRegistered, opt => opt.MapFrom(src => src.Properties.Any(a => a.RegDate < DateTime.Now)));
            

            CreateMap<LegalEntityDto, LegalEntity>();
        }
    }
}