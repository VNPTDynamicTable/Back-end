using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VNPT.SNV.Api.SearchRepo.Dtos;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.SearchRepo
{
    public static class SearchHelper
    {
        public static string SanitizeSearchText(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return searchText;
            var sanitized = Regex.Replace(searchText, @"(--|/\*|\*/|[';])", "");
            return sanitized.Trim();
        }

        public static SearchType DetermineSearchType(string searchText)
        {
            var hasLetters = Regex.IsMatch(searchText, @"[a-zA-ZÀ-ỹ]");
            var hasNumbers = Regex.IsMatch(searchText, @"\d");

            if (hasLetters && hasNumbers)
                return SearchType.Mixed;
            else if (hasNumbers && !hasLetters)
                return SearchType.Mixed;
            else
                return SearchType.Text;
        }

        public static bool IsIdOrReferenceField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return false;
            var fieldNameLower = fieldName.ToLower();
            if (fieldNameLower == "id")
                return true;
            if (fieldNameLower.EndsWith("id") && fieldNameLower.Length > 2)
                return true;
            return false;
        }

        public static List<MetaField> GetSearchableFields(List<MetaField> fields, SearchType searchType)
        {
            var filteredFields = fields.Where(f =>
                !IsIdOrReferenceField(f.FieldNameDB)
            ).ToList();

            switch (searchType)
            {
                case SearchType.Text:
                    return filteredFields.Where(f => IsTextSearchableField(f.DataType)).ToList();

                case SearchType.Number:
                    return filteredFields.Where(f => IsNumericField(f.DataType)).ToList();

                case SearchType.Mixed:
                    return filteredFields.Where(f => IsTextSearchableField(f.DataType) || IsNumericField(f.DataType)).ToList();

                default:
                    return filteredFields;
            }
        }

        public static bool IsTextSearchableField(string dataType)
        {
            var textTypes = new[] { "text", "varchar(20)", "varchar(50)", "varchar(100)" };
            return textTypes.Any(t => dataType.ToLower().Contains(t));
        }

        public static bool IsNumericField(string dataType)
        {
            var numericTypes = new[] { "int4", "int8", "float8", "float4", "decimal(10,2)", "decimal(18,4)", "numeric(10,2)", "numeric(18,4)", "money" };
            return numericTypes.Any(t => dataType.ToLower().Contains(t));
        }

        public static (string Query, Dictionary<string, object> Parameters) BuildSearchQuery(
            MetaTable table, List<MetaField> fields, List<MetaField> searchableFields, string searchText, SearchType searchType, int maxResults)
        {
            var queryBuilder = new StringBuilder();
            var parameters = new Dictionary<string, object>();

            var selectFields = fields.Select(f => $"\"{f.FieldNameDB}\"").ToList();
            if (!selectFields.Contains("\"Id\""))
                selectFields.Insert(0, "\"Id\"");

            queryBuilder.Append($"SELECT {string.Join(", ", selectFields)} FROM \"{table.TableNameDB}\" WHERE ");

            var conditions = new List<string>();
            var paramIndex = 0;

            foreach (var field in searchableFields)
            {
                switch (searchType)
                {
                    case SearchType.Text:
                        if (IsTextSearchableField(field.DataType))
                        {
                            var paramName = $"search_text_{paramIndex++}";
                            conditions.Add($"\"{field.FieldNameDB}\" ILIKE @{paramName}");
                            parameters.Add(paramName, $"%{searchText}%");
                        }
                        break;

                    case SearchType.Mixed:
                        if (IsTextSearchableField(field.DataType))
                        {
                            var paramName = $"search_mixed_text_{paramIndex++}";
                            conditions.Add($"\"{field.FieldNameDB}\" ILIKE @{paramName}");
                            parameters.Add(paramName, $"%{searchText}%");
                        }

                        if (IsNumericField(field.DataType) && IsNumeric(searchText))
                        {
                            var paramName = $"search_mixed_num_{paramIndex++}";
                            conditions.Add($"\"{field.FieldNameDB}\" = @{paramName}");
                            parameters.Add(paramName, ConvertToNumeric(searchText, field.DataType));
                        }
                        break;
                }
            }

            if (conditions.Any())
            {
                queryBuilder.Append($"({string.Join(" OR ", conditions)}) ");
            }
            else
            {
                queryBuilder.Append("1=0 ");
            }

            queryBuilder.Append($"ORDER BY \"Id\" LIMIT {maxResults}");

            return (queryBuilder.ToString(), parameters);
        }

        public static bool IsNumeric(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        public static object ConvertToNumeric(string value, string dataType)
        {
            switch (dataType.ToLower())
            {
                case "int4":
                    return int.TryParse(value, out int intVal) ? intVal : 0;
                case "int8":
                    return long.TryParse(value, out long longVal) ? longVal : 0L;
                case "float4":
                    return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal) ? floatVal : 0f;
                case "float8":
                    return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleVal) ? doubleVal : 0.0;
                case "decimal(10,2)":
                case "decimal(18,4)":
                case "numeric(10,2)":
                case "numeric(18,4)":
                case "money":
                    return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalVal) ? decimalVal : 0m;
                default:
                    return value;
            }
        }

        public static (string Query, Dictionary<string, object> Parameters) BuildCountQuery(
            MetaTable table, List<MetaField> searchableFields, string searchText, SearchType searchType)
        {
            var queryBuilder = new StringBuilder();
            var parameters = new Dictionary<string, object>();

            queryBuilder.Append($"SELECT COUNT(*) FROM \"{table.TableNameDB}\" WHERE ");

            var conditions = new List<string>();
            var paramIndex = 0;

            foreach (var field in searchableFields)
            {
                switch (searchType)
                {
                    case SearchType.Text:
                        if (IsTextSearchableField(field.DataType))
                        {
                            var paramName = $"count_text_{paramIndex++}";
                            conditions.Add($"\"{field.FieldNameDB}\" ILIKE @{paramName}");
                            parameters.Add(paramName, $"%{searchText}%");
                        }
                        break;

                    case SearchType.Mixed:
                        if (IsTextSearchableField(field.DataType))
                        {
                            var paramName = $"count_mixed_text_{paramIndex++}";
                            conditions.Add($"\"{field.FieldNameDB}\" ILIKE @{paramName}");
                            parameters.Add(paramName, $"%{searchText}%");
                        }
                        if (IsNumericField(field.DataType) && IsNumeric(searchText))
                        {
                            var paramName = $"count_mixed_num_{paramIndex++}";
                            conditions.Add($"\"{field.FieldNameDB}\" = @{paramName}");
                            parameters.Add(paramName, ConvertToNumeric(searchText, field.DataType));
                        }
                        break;
                }
            }

            if (conditions.Any())
            {
                queryBuilder.Append($"({string.Join(" OR ", conditions)})");
            }
            else
            {
                queryBuilder.Append("1=0");
            }

            return (queryBuilder.ToString(), parameters);
        }
    }
}
