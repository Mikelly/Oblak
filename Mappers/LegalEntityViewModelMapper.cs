using AutoMapper;
using Oblak.Data;
using Oblak.Models;

namespace Oblak.Mappers
{
    public class LegalEntityMapper : Profile
    {
        public LegalEntityMapper() {
            CreateMap<LegalEntity, LegalEntityViewModel>()
                .ForMember(dest => dest.IsRegistered, opt => opt.MapFrom(src => src.Properties.All(a => a.RegDate != null && a.RegDate >= DateTime.Now.Date)))
                ;

            CreateMap<LegalEntityViewModel, LegalEntity>();
        }
    }
}