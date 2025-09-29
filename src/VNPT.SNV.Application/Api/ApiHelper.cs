using Abp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api
{
    public static class ApiHelper
    {
        public static void ValidateIdentifier(string ident, string errMessage1 = "Tên không hợp lệ!", string errMessage2 = "Định dạng tên không hợp lệ!")
        {
            if (string.IsNullOrWhiteSpace(ident))
                throw new UserFriendlyException(errMessage1);
            if (!Regex.IsMatch(ident, "^[a-zA-Z_][a-zA-Z0-9_]*$"))
                throw new UserFriendlyException(errMessage2);
        }

        public static string FormatDefaultForSql(string value, string dataType)
        {
            dataType = dataType.ToLower();

            if (dataType.StartsWith("varchar") || dataType.StartsWith("char") ||
                dataType == "text" || dataType.StartsWith("character"))
                return $"'{value.Replace("'", "''")}'";

            if (dataType == "boolean" || dataType == "bool")
                return value.ToLower() is "true" or "1" or "t" or "yes" ? "TRUE" : "FALSE";

            if (dataType is "smallint" or "integer" or "bigint" or "int2" or "int4" or "int8" ||
                dataType is "decimal" or "numeric" or "real" or "double precision" or "float4" or "float8")
                return value; 

            if (dataType is "date" or "timestamp" or "timestamp without time zone" or "timestamp with time zone" or "time" or "time without time zone" or "time with time zone")
                return $"'{value}'"; 

            if (dataType == "uuid")
                return $"'{value}'";

            if (dataType is "json" or "jsonb")
                return $"'{value.Replace("'", "''")}'";

            if (dataType.EndsWith("[]"))
                return $"ARRAY{value}";

            if (dataType == "bytea")
                return $"E'\\\\x{value}'"; 

            if (dataType == "money")
                return $"'{value}'";

            if (dataType == "interval")
                return $"'{value}'";

            return value;
        }
    }
}
