using Abp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.RelationRepo.Dtos;

namespace VNPT.SNV.Api.RelationRepo
{
    public interface IRelationAppService : IApplicationService
    {
        Task<List<RelationDto>> GetByTableAsync(string tableName);
        Task<List<RelationDto>> CreateRelationAsync(List<CreateRelationDto> inputs);
        Task DeleteRelationAsync([FromBody] List<CreateRelationDto> input);
    }
}
