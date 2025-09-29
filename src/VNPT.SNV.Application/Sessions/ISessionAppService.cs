using Abp.Application.Services;
using VNPT.SNV.Sessions.Dto;
using System.Threading.Tasks;

namespace VNPT.SNV.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
