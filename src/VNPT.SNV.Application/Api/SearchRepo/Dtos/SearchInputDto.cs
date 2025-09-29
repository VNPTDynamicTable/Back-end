using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.SearchRepo.Dtos
{
    public class SearchInputDto
    {
        public string SearchText { get; set; }
        public int MaxResultsPerTable { get; set; } = 100;
        public int MaxTotalResults { get; set; } = 1000;
    }
}
