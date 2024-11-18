using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class TaxPaymentDtoMapper : Profile
    {
        public TaxPaymentDtoMapper() {
            CreateMap<TaxPayment, TaxPaymentDto>().ReverseMap();
        }
    }
}