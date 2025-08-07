using AutoMapper;
using Oblak.Data;
using Oblak.Models;

namespace Oblak.Mappers
{
    public class LegalEntityMapper : Profile
    {
        public LegalEntityMapper()
        {
            CreateMap<LegalEntity, LegalEntityViewModel>()
                .ForMember(
                    dest => dest.IsRegistered,
                    opt => opt.MapFrom(src =>
                        src.Properties != null
                        && src.Properties.Any()
                        && src.Properties.All(p =>
                            p.RegDate != null
                            && p.RegDate.Value.Date >= DateTime.Now.Date
                        )
                    )
                );

            CreateMap<LegalEntityViewModel, LegalEntity>();
        }
    }

}