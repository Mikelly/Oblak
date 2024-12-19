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
            CreateMap<Group, GroupEnrichedDto>()
                .ForMember(dest => dest.CheckInPointName, opt => opt.MapFrom(src => src.CheckInPoint == null ? "" : src.CheckInPoint!.Name))
                .ForMember(dest => dest.ResTaxPaymentTypeName, opt => opt.MapFrom(src => src.ResTaxPaymentType == null ? "" : src.ResTaxPaymentType!.Description))
                ;
        }
    }
}