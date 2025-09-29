using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.DatasRepo.Dtos
{
    public class CreateDataInputDto
    {
        [Required]
        public int TableId { get; set; }

        [Required]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public bool ValidateData { get; set; } = true;
        public bool AutoGenerateId { get; set; } = true;
    }
}
