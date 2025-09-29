using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.SearchRepo.Dtos
{
    public class SearchResultDto
    {
        public string SearchText { get; set; }
        public SearchType SearchType { get; set; }
        public int TotalTables { get; set; }
        public int TotalResults { get; set; }
        public List<TableSearchResultDto> TableResults { get; set; } = new List<TableSearchResultDto>();
        public DateTime SearchTime { get; set; }
        public string Message { get; set; }
    }

    public enum SearchType
    {
        Text,
        Number,
        Mixed
    }
}
