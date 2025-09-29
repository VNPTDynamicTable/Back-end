using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace VNPT.SNV.EntityFrameworkCore;

public static class SNVDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<SNVDbContext> builder, string connectionString)
    {
        builder.UseNpgsql(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<SNVDbContext> builder, DbConnection connection)
    {
        builder.UseNpgsql(connection);
    }
}
