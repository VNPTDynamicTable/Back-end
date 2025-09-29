using Abp.Application.Services;
using Abp.Application.Services.Dto;
using VNPT.SNV.Roles.Dto;
using VNPT.SNV.Users.Dto;
using System.Threading.Tasks;

namespace VNPT.SNV.Users;

public interface IUserAppService : IAsyncCrudAppService<UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>
{
    Task DeActivate(EntityDto<long> user);
    Task Activate(EntityDto<long> user);
    Task<ListResultDto<RoleDto>> GetRoles();
    Task ChangeLanguage(ChangeUserLanguageDto input);

    Task<bool> ChangePassword(ChangePasswordDto input);
}
