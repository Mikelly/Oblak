using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class ExcursionInvoiceItemDtoMapper : Profile
    {
        public ExcursionInvoiceItemDtoMapper() {
            CreateMap<ExcursionInvoiceItem, ExcursionInvoiceItemDto>()
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Price * src.NoOfPersons));

            CreateMap<ExcursionInvoiceItemDto, ExcursionInvoiceItem>();
        }
    }
}