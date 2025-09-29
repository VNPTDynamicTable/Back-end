using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.TableRepo.Dtos;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.FieldRepo.Dtos
{
    public class FieldMapProfile : Profile
    {
        public FieldMapProfile()
        {
            CreateMap<MetaField, FieldDto>();

            CreateMap<FieldDto, MetaField>()
                .ForMember(dest => dest.metaTable, opt => opt.Ignore());

            CreateMap<CreateFieldDto, MetaField>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.metaTable, o => o.Ignore());

            CreateMap<UpdateTableDto, MetaField>()
                .ForMember(d => d.metaTable, o => o.Ignore());
        }
    }
}
