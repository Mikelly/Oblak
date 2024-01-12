using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.rb90;
using Oblak.Models.Srb;

namespace Oblak.Mappers
{
    public class SrbPersonViewModelMapper : Profile
    {
        public SrbPersonViewModelMapper() {
            CreateMap<SrbPerson, Models.Srb.SrbPersonViewModel>();
        }
    }
}