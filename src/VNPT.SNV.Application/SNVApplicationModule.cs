using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using VNPT.SNV.Authorization;

namespace VNPT.SNV;

[DependsOn(
    typeof(SNVCoreModule),
    typeof(AbpAutoMapperModule))]
public class SNVApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<SNVAuthorizationProvider>();
    }

    public override void Initialize()
    {
        var thisAssembly = typeof(SNVApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }
}
