using Abp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.DatasRepo.Dtos;

namespace VNPT.SNV.Api.DatasRepo
{
    public interface IDatasAppService : IApplicationService
    {
        Task<PagedResultDto<DataDto>> GetDataByTableIdAsync([FromBody] GetDataInputDto input);
        Task<DataDto> GetByIdAsync([FromBody] GetDataByIdDto input);
        Task<CreateDataResultDto> CreateDataAsync(CreateDataInputDto input);
        Task <UpdateDataResultDto> UpdateDataAsync(UpdateDataInputDto input);
        Task<List<DataDto>> GetDataByFieldAsync(string tableName, string fieldName);
        Task DeleteDataAsync([FromQuery] int tableId, [FromQuery] List<int> ids);
        Task<int> GetTotalRecordsCountAsync();
        Task<int> GetTotalPublicCountAsync();
        Task<DataGrowthSummaryDto> GetDataGrowthLast7DaysAsync();
        Task<List<TopTableDto>> GetTop5TablesAsync();
    }
}
