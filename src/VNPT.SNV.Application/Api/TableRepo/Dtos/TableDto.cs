using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.TableRepo.Dtos
{
    public class TableDto : IEntityDto<int>
    {
        public int Id { get; set; }
        public string TableNameDB { get; set; }
        public string DisplayNameVN { get; set; }
        public bool IsPublic { get; set; } = false;
    }
}
