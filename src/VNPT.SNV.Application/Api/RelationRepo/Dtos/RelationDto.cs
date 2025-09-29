using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.RelationRepo.Dtos
{
    public class RelationDto : IEntityDto<int>
    {
        public int Id { get; set; }
        public string? ConstraintName { get; set; }
        public string SourceTable { get; set; }
        public string SourceField { get; set; }
        public string TargetTable { get; set; }
        public string TargetField { get; set; }
        public string? TypeUpdate { get; set; }
        public string? TypeDelete { get; set; }
    }
}
