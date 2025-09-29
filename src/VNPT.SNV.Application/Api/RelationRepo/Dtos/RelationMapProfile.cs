using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.RelationRepo.Dtos
{
    public class RelationMapProfile : Profile
    {
        public RelationMapProfile()
        {
            CreateMap<CreateRelationDto, RelationDto>();
        }
    }
}
