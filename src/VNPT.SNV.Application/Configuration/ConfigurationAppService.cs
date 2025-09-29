using Abp.Authorization;
using Abp.Runtime.Session;
using VNPT.SNV.Configuration.Dto;
using System.Threading.Tasks;

namespace VNPT.SNV.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : SNVAppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
