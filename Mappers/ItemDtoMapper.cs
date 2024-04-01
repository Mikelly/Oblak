using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class ItemDtoMapper : Profile
    {
        public ItemDtoMapper() {
            CreateMap<Item, ItemDto>().ReverseMap();
        }
    }
}