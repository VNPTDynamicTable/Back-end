using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.RelationRepo.Dtos;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.DatasRepo.Dtos
{
    public class GetDataByIdDto
    {
        [Required]
        public int TableId { get; set; }
        [Required]
        public int DataId { get; set; }
        public List<RelationDto> relations { get; set; } = new List<RelationDto>();
        public List<MetaField>? selectedFields { get; set; } = new List<MetaField>();
    }
}
