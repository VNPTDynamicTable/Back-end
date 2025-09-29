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
    public class MetaFieldConfiguration : IEntityTypeConfiguration<MetaField>
    {
        public void Configure(EntityTypeBuilder<MetaField> builder)
        {
            builder.ToTable("SNVMetaField");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FieldNameDB).IsUnicode(false).HasMaxLength(50);
            builder.Property(x => x.DisplayNameVN).HasMaxLength(50);
            builder.Property(x => x.TargetField).IsUnicode(false).HasMaxLength(50);
            builder.HasIndex(x => x.TableId);
            builder.HasIndex(x => new { x.TableId, x.FieldNameDB }).IsUnique();
            builder.HasIndex(x => new { x.TableId, x.DisplayNameVN }).IsUnique();
            builder.HasOne(x => x.metaTable)
                .WithMany(x => x.metaFields)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
