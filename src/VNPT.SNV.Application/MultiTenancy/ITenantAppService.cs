using Abp.Application.Services;
using VNPT.SNV.MultiTenancy.Dto;

namespace VNPT.SNV.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}

