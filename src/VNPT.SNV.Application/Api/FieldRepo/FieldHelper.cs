using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.FieldRepo
{
    public class FieldHelper
    {
        public static string AddFieldSql(string tableName, MetaField field)
        {
            ApiHelper.ValidateIdentifier(tableName, "Tên bảng không hợp lệ!", "Định dạng tên bảng không hợp lệ!");
            ApiHelper.ValidateIdentifier(field.FieldNameDB, "Tên field không hợp lệ!", "Định dạng tên field không hợp lệ!");

            var sb = new StringBuilder();
            sb.Append($@"ALTER TABLE ""{tableName}"" ADD COLUMN ""{field.FieldNameDB}"" {field.DataType}");

            if (field.IsRequired)
                sb.Append(" NOT NULL");

            if (field.IsUnique)
                sb.Append(" UNIQUE");

            var DefaultValue = field.DefaultValue;

            if (!string.IsNullOrEmpty(DefaultValue) && DefaultValue.Contains("Khóa ngoại của"))
            {
                DefaultValue = "";
            }

            if (!string.IsNullOrWhiteSpace(DefaultValue))
            {
                var formatted = ApiHelper.FormatDefaultForSql(DefaultValue, field.DataType);
                sb.Append($" DEFAULT {formatted}");
            }

            sb.Append(";");
            return sb.ToString();
        }

        public static string DeleteFieldSql(string tableName, MetaField field, bool cascade = true)
        {
            ApiHelper.ValidateIdentifier(tableName, "Tên bảng không hợp lệ!", "Định dạng tên bảng không hợp lệ!");
            ApiHelper.ValidateIdentifier(field.FieldNameDB, "Tên field không hợp lệ!", "Định dạng tên field không hợp lệ!");

            var sql = $@"ALTER TABLE ""{tableName}"" DROP COLUMN IF EXISTS ""{field.FieldNameDB}""";

            if (cascade)
                sql += " CASCADE";

            sql += ";";
            return sql;
        }

        public static List<string> UpdateFieldSql(string tableName, MetaField oldField, MetaField newField)
        {
            var sqlList = new List<string>();

            ApiHelper.ValidateIdentifier(tableName, "Tên bảng không hợp lệ!", "Định dạng tên bảng không hợp lệ!");
            ApiHelper.ValidateIdentifier(newField.FieldNameDB, "Tên field không hợp lệ!", "Định dạng tên field không hợp lệ!");

            if (oldField.FieldNameDB != newField.FieldNameDB)
            {
                sqlList.Add($@"ALTER TABLE ""{tableName}"" RENAME COLUMN ""{oldField.FieldNameDB}"" TO ""{newField.FieldNameDB}"";");
            }

            if (!string.Equals(oldField.DataType, newField.DataType, StringComparison.OrdinalIgnoreCase))
            {
                sqlList.Add($@"ALTER TABLE ""{tableName}"" ALTER COLUMN ""{newField.FieldNameDB}"" TYPE {newField.DataType};");
            }

            if (oldField.IsRequired != newField.IsRequired)
            {
                if (newField.IsRequired)
                    sqlList.Add($@"ALTER TABLE ""{tableName}"" ALTER COLUMN ""{newField.FieldNameDB}"" SET NOT NULL;");
                else
                    sqlList.Add($@"ALTER TABLE ""{tableName}"" ALTER COLUMN ""{newField.FieldNameDB}"" DROP NOT NULL;");
            }

            if (oldField.DefaultValue != newField.DefaultValue)
            {
                if (!string.IsNullOrWhiteSpace(newField.DefaultValue))
                {
                    var formatted = ApiHelper.FormatDefaultForSql(newField.DefaultValue, newField.DataType);
                    sqlList.Add($@"ALTER TABLE ""{tableName}"" ALTER COLUMN ""{newField.FieldNameDB}"" SET DEFAULT {formatted};");
                }
                else
                {
                    sqlList.Add($@"ALTER TABLE ""{tableName}"" ALTER COLUMN ""{newField.FieldNameDB}"" DROP DEFAULT;");
                }
            }

            if (oldField.IsUnique != newField.IsUnique)
            {
                var constraintName = $"UQ_{tableName}_{newField.FieldNameDB}";
                var oldConstraintName = $"UQ_{tableName}_{oldField.FieldNameDB}";
                if (newField.IsUnique)
                {
                    if (oldField.FieldNameDB != newField.FieldNameDB)
                    {
                        sqlList.Add($@"ALTER TABLE ""{tableName}"" DROP CONSTRAINT IF EXISTS ""{oldConstraintName}"";");
                    }

                    sqlList.Add($@"ALTER TABLE ""{tableName}"" ADD CONSTRAINT ""{constraintName}"" UNIQUE (""{newField.FieldNameDB}"");");
                }
                else
                {
                    sqlList.Add($@"ALTER TABLE ""{tableName}"" DROP CONSTRAINT IF EXISTS ""{oldConstraintName}"";");
                }
            }

            return sqlList;
        }

    }
}
