using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class ResTaxFeeDtoMapper : Profile
    {
        public ResTaxFeeDtoMapper() {

            CreateMap<ResTaxFee, ResTaxFeeDto>()
				.ForMember(dest => dest.ResTaxPaymentType, opt => opt.MapFrom(src => src.ResTaxPaymentType.Description)); 

			CreateMap<ResTaxFeeDto, ResTaxFee>();
		}
    }
}