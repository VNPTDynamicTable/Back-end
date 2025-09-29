using Abp.Zero.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using VNPT.SNV.Authorization.Roles;
using VNPT.SNV.Authorization.Users;
using VNPT.SNV.Configuration;
using VNPT.SNV.Models;
using VNPT.SNV.MultiTenancy;

namespace VNPT.SNV.EntityFrameworkCore;

public class SNVDbContext : AbpZeroDbContext<Tenant, Role, User, SNVDbContext>
{
    /* Define a DbSet for each entity of the application */
    //Demo
    public DbSet<MetaTable> MetaTables { get; set; }
    public DbSet<MetaField> MetaFields { get; set; }

    public SNVDbContext(DbContextOptions<SNVDbContext> options)
        : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Configure your own tables/entities inside here */
        builder.ApplyConfiguration(new MetaTableConfiguration());
        builder.ApplyConfiguration(new MetaFieldConfiguration());

    }
}
