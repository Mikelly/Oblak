using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class TaxPaymentBalanceDtoMapper : Profile
    {
        public TaxPaymentBalanceDtoMapper() {
            CreateMap<TaxPaymentBalance, TaxPaymentBalanceDto>().ReverseMap();
        }
    }
}