using AutoMapper;
using Oblak.Data;
using Oblak.Models.Api;
using Oblak.Models.rb90;

namespace Oblak.Mappers
{
    public class SrbPersonDtoMapper : Profile
    {
        public SrbPersonDtoMapper() {
            CreateMap<SrbPerson, SrbPersonDto>()
                .ReverseMap();
        }
    }
}