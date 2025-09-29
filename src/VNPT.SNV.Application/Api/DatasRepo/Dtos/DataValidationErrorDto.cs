using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.DatasRepo.Dtos
{
    public class DataValidationErrorDto
    {
        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }
        public object InvalidValue { get; set; }
    }

    public class DataValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<DataValidationErrorDto> Errors { get; set; } = new List<DataValidationErrorDto>();
    }
}
