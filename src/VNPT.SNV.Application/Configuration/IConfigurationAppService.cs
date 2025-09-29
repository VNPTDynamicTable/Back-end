using VNPT.SNV.Configuration.Dto;
using System.Threading.Tasks;

namespace VNPT.SNV.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
