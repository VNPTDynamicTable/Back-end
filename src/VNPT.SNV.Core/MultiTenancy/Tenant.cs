using Abp.MultiTenancy;
using VNPT.SNV.Authorization.Users;

namespace VNPT.SNV.MultiTenancy;

public class Tenant : AbpTenant<User>
{
    public Tenant()
    {
    }

    public Tenant(string tenancyName, string name)
        : base(tenancyName, name)
    {
    }
}
