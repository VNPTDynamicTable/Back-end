using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.DatasRepo.Dtos
{
    public class TopTableDto
    {
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public int RecordCount { get; set; }
        public DateTime? LastModificationTime { get; set; }
        public bool IsPublic { get; set; }
    }
}
