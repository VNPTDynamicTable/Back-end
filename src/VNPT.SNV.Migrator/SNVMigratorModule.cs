using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using VNPT.SNV.Configuration;
using VNPT.SNV.EntityFrameworkCore;
using VNPT.SNV.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace VNPT.SNV.Migrator;

[DependsOn(typeof(SNVEntityFrameworkModule))]
public class SNVMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public SNVMigratorModule(SNVEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(SNVMigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            SNVConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(SNVMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
