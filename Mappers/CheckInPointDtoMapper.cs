using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class CheckInPointDtoMapper : Profile
    {
        public CheckInPointDtoMapper() {
            CreateMap<CheckInPoint, CheckInPointDto>().ReverseMap();
        }
    }
}