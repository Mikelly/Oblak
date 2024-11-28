using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class UserDtoMapper : Profile
    {
        public UserDtoMapper() {
            CreateMap<ApplicationUser, UserDto>().ReverseMap();
        }
    }
}