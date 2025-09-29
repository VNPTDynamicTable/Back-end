using Abp.Application.Services;
using VNPT.SNV.Authorization.Accounts.Dto;
using System.Threading.Tasks;

namespace VNPT.SNV.Authorization.Accounts;

public interface IAccountAppService : IApplicationService
{
    Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

    Task<RegisterOutput> Register(RegisterInput input);
}
