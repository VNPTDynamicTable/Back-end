using Abp.Domain.Entities.Auditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Models
{
    public class MetaField : AuditedEntity<int>
    {
        public string FieldNameDB { get; set; }
        public string DisplayNameVN {  get; set; }
        public string DataType {  get; set; }
        public bool IsRequired { get; set; } = false;
        public bool IsUnique { get; set; } = false;
        public string? DefaultValue { get; set; }
        public bool IsForeignKey { get; set; } = false;
        public string? TargetField { get; set; } = string.Empty;

        public int TableId { get; set; }
        public MetaTable metaTable { get; set; }

    }
}
