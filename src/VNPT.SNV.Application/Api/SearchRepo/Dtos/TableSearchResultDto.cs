using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.SearchRepo.Dtos
{
    public class TableSearchResultDto
    {
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public List<Dictionary<string, object>> Results { get; set; } = new List<Dictionary<string, object>>();
        public int TotalCount { get; set; }
    }
}
