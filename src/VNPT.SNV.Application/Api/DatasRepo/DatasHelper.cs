using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNPT.SNV.Api.DatasRepo
{
    public static class DatasHelper
    {
        public static bool IsValidDataType(object value, string dataType)
        {
            try
            {
                switch (dataType.ToLower())
                {
                    case "int4":
                        Convert.ToInt32(value);
                        break;
                    case "int8":
                        Convert.ToInt64(value);
                        break;
                    case "float4":
                        Convert.ToSingle(value);
                        break;
                    case "float8":
                        Convert.ToDouble(value);
                        break;
                    case "decimal(10,2)":
                    case "decimal(18,4)":
                    case "numeric(10,2)":
                    case "numeric(18,4)":
                    case "money":
                        Convert.ToDecimal(value);
                        break;
                    case "boolean":
                        Convert.ToBoolean(value);
                        break;
                    case "time":
                        TimeSpan.Parse(value.ToString());
                        break;
                    case "datetime":
                    case "timestamp":
                        Convert.ToDateTime(value);
                        break;
                    case "date":
                        DateTime.Parse(value.ToString());
                        break;
                    default:
                        return true;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object ConvertToDataType(string value, string dataType)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            try
            {
                switch (dataType.ToLower())
                {
                    case "int4":
                        return Convert.ToInt32(value);
                    case "int8":
                        return Convert.ToInt64(value);
                    case "float4":
                        return Convert.ToSingle(value);
                    case "float8":
                        return Convert.ToDouble(value);
                    case "boolean":
                        return Convert.ToBoolean(value);
                    case "date":
                        return DateTime.Parse(value).Date;
                    case "time":
                        return TimeSpan.Parse(value);
                    case "timestamp":
                        return DateTime.Parse(value);
                    case "decimal(10,2)":
                    case "decimal(18,4)":
                    case "numeric(10,2)":
                    case "numeric(18,4)":
                    case "money":
                        return Convert.ToDecimal(value);
                    default:
                        return value;
                }
            }
            catch
            {
                return value;
            }
        }

        public static string BuildInsertQuery(string tableName, Dictionary<string, object> data)
        {
            var columns = string.Join(", ", data.Keys.Select(k => $"\"{k}\""));
            var values = string.Join(", ", data.Keys.Select(k => $"@{k}"));

            return $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({values}) RETURNING \"Id\"";
        }

        public static string BuildUpdateQuery(string tableName, Dictionary<string, object> data)
        {
            var setClause = string.Join(", ", data.Keys.Select(k => $"\"{k}\" = @{k}"));
            return $"UPDATE \"{tableName}\" SET {setClause} WHERE \"Id\" = @Id";
        }
    }
}
