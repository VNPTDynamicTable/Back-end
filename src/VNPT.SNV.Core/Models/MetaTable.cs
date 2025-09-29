using Abp.Domain.Entities.Auditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Models
{
    public class MetaTable : AuditedAggregateRoot<int>
    {
        public string TableNameDB { get; set; }
        public string DisplayNameVN {  get; set; }
        public bool IsPublic { get; set; } = false;
        public List<MetaField> metaFields { get; set; } = new();
    }
}
