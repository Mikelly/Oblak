using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Models.EFI;
using Oblak.Models.Srb;

namespace Oblak.Mappers
{
    public class GroupDtoMapper : Profile
    {
        public GroupDtoMapper() {
            CreateMap<Group, GroupDto>()
                .ReverseMap();
        }
    }

    public class GroupEnrichedDtoMapper : Profile
    {
        public GroupEnrichedDtoMapper()
        {
            CreateMap<Group, GroupEnrichedDto>();
        }
    }
}