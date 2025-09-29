using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace VNPT.SNV.Controllers
{
    public abstract class SNVControllerBase : AbpController
    {
        protected SNVControllerBase()
        {
            LocalizationSourceName = SNVConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
