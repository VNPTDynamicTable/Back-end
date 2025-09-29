using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.FieldRepo.Dtos
{
    public class FieldDto : IEntityDto<int>
    {
        public int Id { get; set; }
        public string FieldNameDB { get; set; }
        public string DisplayNameVN { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsUnique { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsForeignKey { get; set; } = false;
        public string? TargetField { get; set; } = string.Empty;

        public int TableId { get; set; }
    }
}
