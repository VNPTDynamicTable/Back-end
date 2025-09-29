using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.DatasRepo.Dtos
{
    public class DataDto : IEntityDto<int>
    {
        public int Id { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
