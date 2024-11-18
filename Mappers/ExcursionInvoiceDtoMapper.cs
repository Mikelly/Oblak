using AutoMapper;
using Oblak.Data;
using Oblak.Data.Api;
using Oblak.Data.Enums;
using Oblak.Models;
using Oblak.Models.Api;

namespace Oblak.Mappers
{
    public class ExcursionInvoiceDtoMapper : Profile
    {
        public ExcursionInvoiceDtoMapper() {

            CreateMap<ExcursionInvoice, ExcursionInvoiceDto>()
                .ForMember(dest => dest.BillingTotal, opt => opt.MapFrom(src => src.BillingAmount + src.BillingFee))
                .ForMember(dest => dest.AgencyName, opt => opt.MapFrom(src => src.Agency.Name))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status == TaxInvoiceStatus.Active ? "U izradi" : src.Status == TaxInvoiceStatus.Closed ? "Zaključena" : src.Status == TaxInvoiceStatus.Opened ? "Otvorena" : "Stornirana"))
                .ForMember(dest => dest.TaxPaymentTypeDescription, opt => opt.MapFrom(src => src.TaxPaymentType.Description));

            CreateMap<ExcursionInvoiceDto, ExcursionInvoice>();
        }
    }
}