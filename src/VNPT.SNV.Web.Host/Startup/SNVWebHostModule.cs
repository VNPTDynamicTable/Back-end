using Abp.Modules;
using Abp.Reflection.Extensions;
using VNPT.SNV.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace VNPT.SNV.Web.Host.Startup
{
    [DependsOn(
       typeof(SNVWebCoreModule))]
    public class SNVWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public SNVWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(SNVWebHostModule).GetAssembly());
        }
    }
}
