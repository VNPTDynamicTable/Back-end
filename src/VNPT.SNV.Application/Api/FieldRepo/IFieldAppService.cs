using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.FieldRepo.Dtos;

namespace VNPT.SNV.Api.FieldRepo
{
    public interface IFieldAppService : ICrudAppService<
        FieldDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateFieldDto,
        UpdateFieldDto>
    {
        public Task<List<FieldDto>> GetByTableIdAsync(int tableId);
        public Task<List<FieldDto>> CreateFieldAsync (List<CreateFieldDto> inputs);
        public Task<List<FieldDto>> UpdateFieldAsync(List<UpdateFieldDto> inputs);
        public Task DeleteFieldAsync([FromQuery] List<int> ids);
    }
}
