using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using VNPT.SNV.EntityFrameworkCore;
using VNPT.SNV.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace VNPT.SNV.Web.Tests;

[DependsOn(
    typeof(SNVWebMvcModule),
    typeof(AbpAspNetCoreTestBaseModule)
)]
public class SNVWebTestModule : AbpModule
{
    public SNVWebTestModule(SNVEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
    }

    public override void PreInitialize()
    {
        Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(SNVWebTestModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<ApplicationPartManager>()
            .AddApplicationPartsIfNotAddedBefore(typeof(SNVWebMvcModule).Assembly);
    }
}