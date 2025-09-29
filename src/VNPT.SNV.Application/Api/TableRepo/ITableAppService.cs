using Abp.Application.Services;
using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.TableRepo.Dtos;

namespace VNPT.SNV.Api.TableRepo
{
    public interface ITableAppService : ICrudAppService<
        TableDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateTableDto,
        UpdateTableDto>
    {
        public Task<TableDto> CreateTableAsync(CreateTableDto input);
        public Task<TableDto> UpdateTableAsync(UpdateTableDto input);
        public Task DeleteTableAsync(int id);
    }
}
