using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.DatasRepo.Dtos
{
    public class DataGrowthDto
    {
        public DateTime Date { get; set; }
        public int RecordCount { get; set; }
    }

    public class DataGrowthSummaryDto
    {
        public List<DataGrowthDto> DailyGrowth { get; set; } = new List<DataGrowthDto>();
        public int TotalRecordsLast7Days { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
