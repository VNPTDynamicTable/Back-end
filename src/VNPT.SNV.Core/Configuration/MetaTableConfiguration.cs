using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Models;

namespace VNPT.SNV.Configuration
{
    public class MetaTableConfiguration : IEntityTypeConfiguration<MetaTable>
    {
        public void Configure(EntityTypeBuilder<MetaTable> builder)
        {
            builder.ToTable("SNVMetaTable");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TableNameDB).IsRequired().IsUnicode(false).HasMaxLength(50);
            builder.Property(x => x.DisplayNameVN).IsRequired().HasMaxLength(100);
            builder.HasIndex(x => x.TableNameDB).IsUnique(true);
            builder.HasIndex(x => x.DisplayNameVN).IsUnique(true);
        }
    }
}
