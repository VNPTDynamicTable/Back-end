using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.TableRepo.Dtos
{
    public class TableMapProfile : Profile
    {
        public TableMapProfile()
        {
            CreateMap<MetaTable, TableDto>();

            CreateMap<TableDto, MetaTable>()
                .ForMember(dest => dest.metaFields, opt => opt.Ignore());

            CreateMap<CreateTableDto, MetaTable>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.metaFields, o => o.Ignore());

            CreateMap<UpdateTableDto, MetaTable>()
                .ForMember(d => d.metaFields, o => o.Ignore());
        }
    }
}
