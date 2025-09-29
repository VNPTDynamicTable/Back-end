using Abp.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.SearchRepo.Dtos;

namespace VNPT.SNV.Api.SearchRepo
{
    public interface ISearchAppService : IApplicationService
    {
        Task<SearchResultDto> SearchAsync(SearchInputDto input);
    }
}
