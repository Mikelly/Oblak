﻿using AutoMapper;
using Oblak.Data;
using Oblak.Models;
using Oblak.Models.Api;
using Oblak.Models.rb90;
using Oblak.Models.Srb;

namespace Oblak.Mappers
{
    public class PropertyDtoMapper : Profile
    {
        public PropertyDtoMapper() {
            CreateMap<Property, PropertyDto>();
        }
    }
}