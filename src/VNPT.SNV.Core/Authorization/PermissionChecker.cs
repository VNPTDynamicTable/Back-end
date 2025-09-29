using Abp.Authorization;
using VNPT.SNV.Authorization.Roles;
using VNPT.SNV.Authorization.Users;

namespace VNPT.SNV.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
