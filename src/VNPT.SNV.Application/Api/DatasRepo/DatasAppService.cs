using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.DatasRepo.Dtos;
using VNPT.SNV.Api.FieldRepo;
using VNPT.SNV.Api.RelationRepo;
using VNPT.SNV.Api.RelationRepo.Dtos;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.DatasRepo
{
    [Authorize]
    public class DatasAppService : ApplicationService, IDatasAppService
    {
        private readonly IRepository<MetaTable, int> _tableRepository;
        private readonly IRepository<MetaField, int> _fieldRepository;
        private readonly IRelationAppService _relationAppService;
        private readonly IConfiguration _configuration;

        public DatasAppService(
            IRepository<MetaTable, int> tableRepository,
            IRepository<MetaField, int> fieldRepository,
            IRelationAppService relationAppService,
            IConfiguration configuration)
        {
            _tableRepository = tableRepository;
            _fieldRepository = fieldRepository;
            _relationAppService = relationAppService;
            _configuration = configuration;
        }

        public async Task<CreateDataResultDto> CreateDataAsync(CreateDataInputDto input)
        {
            try
            {
                var metaTable = await _tableRepository.FirstOrDefaultAsync(input.TableId);
                if (metaTable == null)
                {
                    throw new UserFriendlyException($"Không tìm thấy bảng với Id = {input.TableId}!");
                }

                var metaFields = await _fieldRepository.GetAllListAsync(f => f.TableId == input.TableId);
                if (!metaFields.Any())
                {
                    throw new UserFriendlyException($"Bảng '{metaTable.DisplayNameVN}' không có field nào");
                }

                if (input.ValidateData)
                {
                    var validationResult = await ValidateDataAsync(input.Data, metaFields, metaTable.TableNameDB);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = string.Join("; ", validationResult.Errors.Select(e => $"{e.FieldName}: {e.ErrorMessage}"));
                        throw new UserFriendlyException($"Dữ liệu không hợp lệ: {errorMessages}");
                    }
                }

                var insertData = await PrepareInsertDataAsync(input.Data, metaFields, input.AutoGenerateId);

                var insertQuery = DatasHelper.BuildInsertQuery(metaTable.TableNameDB, insertData);

                var newId = await ExecuteInsertAsync(insertQuery, insertData);

                var createdData = await GetCreatedDataAsync(metaTable.TableNameDB, newId, metaFields);

                return new CreateDataResultDto
                {
                    Id = newId,
                    Data = createdData,
                    IsSuccess = true,
                    Message = "Tạo dữ liệu thành công",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi tạo dữ liệu!", ex);
            }
        }

        public async Task<UpdateDataResultDto> UpdateDataAsync(UpdateDataInputDto input)
        {
            try
            {
                var metaTable = await _tableRepository.FirstOrDefaultAsync(input.TableId);
                if (metaTable == null)
                {
                    throw new UserFriendlyException($"Không tìm thấy bảng với Id = {input.TableId}!");
                }

                var metaFields = await _fieldRepository.GetAllListAsync(f => f.TableId == input.TableId);
                if (!metaFields.Any())
                {
                    throw new UserFriendlyException($"Bảng '{metaTable.DisplayNameVN}' không có field nào");
                }

                var existingRecord = await CheckRecordExistsAsync(metaTable.TableNameDB, input.Id);
                if (!existingRecord)
                {
                    throw new UserFriendlyException($"Không tìm thấy bản ghi với Id = {input.Id}!");
                }

                if (input.ValidateData)
                {
                    var validationResult = await ValidateDataForUpdateAsync(input.Data, metaFields, metaTable.TableNameDB, input.Id);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = string.Join("; ", validationResult.Errors.Select(e => $"{e.FieldName}: {e.ErrorMessage}"));
                        throw new UserFriendlyException($"Dữ liệu không hợp lệ: {errorMessages}");
                    }
                }
                var updateData = await PrepareUpdateDataAsync(input.Data, metaFields);

                var updateQuery = BuildUpdateQuery(metaTable.TableNameDB, updateData, input.Id);

                await ExecuteUpdateAsync(updateQuery, updateData, input.Id);

                var updatedData = await GetCreatedDataAsync(metaTable.TableNameDB, input.Id, metaFields);

                return new UpdateDataResultDto
                {
                    Id = input.Id,
                    Data = updatedData,
                    IsSuccess = true,
                    Message = "Cập nhật dữ liệu thành công",
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi cập nhật dữ liệu!", ex);
            }
        }

        [HttpPost]
        public async Task<DataDto> GetByIdAsync([FromBody] GetDataByIdDto input)
        {
            try
            {
                var metaTable = await _tableRepository.FirstOrDefaultAsync(input.TableId);
                if (metaTable == null)
                {
                    throw new UserFriendlyException($"Không tìm thấy bảng với Id = {input.TableId}!");
                }

                var metaFields = await _fieldRepository.GetAllListAsync(f => f.TableId == input.TableId);
                if (!metaFields.Any())
                {
                    throw new UserFriendlyException($"Bảng '{metaTable.DisplayNameVN}' không có field nào");
                }

                var fieldsToSelect = input.selectedFields?.Any() == true
                    ? metaFields.Where(f => input.selectedFields.Any(sf => sf.Id == f.Id)).ToList()
                    : metaFields;

                var queryBuilder = new StringBuilder();
                var selectFields = new List<string>();
                var joinClauses = new List<string>();

                foreach (var field in fieldsToSelect)
                {
                    if (field.IsForeignKey && !string.IsNullOrEmpty(field.TargetField))
                    {
                        if (field.FieldNameDB.EndsWith("Id"))
                        {
                            var originalFieldName = field.FieldNameDB.Substring(0, field.FieldNameDB.Length - 2);
                            var targetTableField = field.TargetField.Split('.');

                            if (targetTableField.Length == 2)
                            {
                                var targetTable = targetTableField[0];
                                var targetField = targetTableField[1];

                                var joinAlias = $"ref_{originalFieldName}";
                                joinClauses.Add($"LEFT JOIN {targetTable} {joinAlias} ON main.\"{field.FieldNameDB}\" = {joinAlias}.\"Id\"");
                                selectFields.Add($"{joinAlias}.\"{targetField}\" AS \"{originalFieldName}\"");
                            }
                        }
                        else
                        {
                            var targetTableField = field.TargetField.Split('.');
                            if (targetTableField.Length == 2)
                            {
                                var targetTable = targetTableField[0];
                                var targetField = targetTableField[1];

                                var joinAlias = $"ref_{field.FieldNameDB}";
                                joinClauses.Add($"LEFT JOIN {targetTable} {joinAlias} ON main.\"{field.FieldNameDB}\" = {joinAlias}.\"{targetField}\"");
                                selectFields.Add($"{joinAlias}.\"{targetField}\" AS \"{field.FieldNameDB}\"");
                            }
                        }
                    }
                    else if (!field.FieldNameDB.EndsWith("Id") || !field.IsForeignKey)
                    {
                        selectFields.Add($"main.\"{field.FieldNameDB}\"");
                    }
                }

                if (!selectFields.Any(s => s.Contains("main.\"Id\"")))
                {
                    selectFields.Insert(0, "main.\"Id\"");
                }

                queryBuilder.Append($"SELECT {string.Join(", ", selectFields)} ");
                queryBuilder.Append($"FROM \"{metaTable.TableNameDB}\" main ");

                if (joinClauses.Any())
                {
                    queryBuilder.Append(string.Join(" ", joinClauses) + " ");
                }

                queryBuilder.Append("WHERE main.\"Id\" = @dataId");

                var query = queryBuilder.ToString();
                var result = await ExecuteQueryAsync(query, new Dictionary<string, object> { { "dataId", input.DataId } });

                if (!result.Any())
                {
                    throw new UserFriendlyException($"Không tìm thấy dữ liệu với Id = {input.DataId}");
                }

                var dataRow = result.First();
                var dataDto = new DataDto
                {
                    Id = Convert.ToInt32(dataRow["Id"]),
                    Data = new Dictionary<string, object>()
                };

                foreach (var kvp in dataRow)
                {
                    if (kvp.Key != "Id")
                    {
                        dataDto.Data[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }

                return dataDto;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi lấy dữ liệu!", ex);
            }
        }

        [HttpPost]
        public async Task<PagedResultDto<DataDto>> GetDataByTableIdAsync([FromBody] GetDataInputDto input)
        {
            try
            {
                var metaTable = await _tableRepository.FirstOrDefaultAsync(input.TableId);
                if (metaTable == null)
                {
                    throw new UserFriendlyException($"Không tìm thấy bảng với Id = {input.TableId}!");
                }

                var metaFields = await _fieldRepository.GetAllListAsync(f => f.TableId == input.TableId);
                if (!metaFields.Any())
                {
                    throw new UserFriendlyException($"Bảng '{metaTable.DisplayNameVN}' không có field nào");
                }

                var fieldsToSelect = input.selectedFields?.Any() == true
                    ? metaFields.Where(f => input.selectedFields.Any(sf => sf.Id == f.Id)).ToList()
                    : metaFields;

                fieldsToSelect = fieldsToSelect
                    .GroupBy(f =>
                        f.FieldNameDB.EndsWith("Id")
                            ? f.FieldNameDB.Substring(0, f.FieldNameDB.Length - 2)
                            : f.FieldNameDB)
                    .Select(g =>
                        g.FirstOrDefault(f => f.FieldNameDB.EndsWith("Id")) ?? g.First())
                    .ToList();

                var countQuery = $"SELECT COUNT(*) FROM \"{metaTable.TableNameDB}\"";
                var totalCount = await ExecuteScalarAsync(countQuery);

                var queryBuilder = new StringBuilder();
                var selectFields = new List<string>();
                var joinClauses = new List<string>();

                foreach (var field in fieldsToSelect)
                {
                    if (field.IsForeignKey && !string.IsNullOrEmpty(field.TargetField))
                    {
                        if (field.FieldNameDB.EndsWith("Id"))
                        {
                            var originalFieldName = field.FieldNameDB.Substring(0, field.FieldNameDB.Length - 2);
                            var relation = input.relations.FirstOrDefault(r => 
                            r.SourceTable == metaTable.TableNameDB && r.SourceField == field.FieldNameDB);
                            var targetTable = relation.TargetTable;
                            var targetField = field.TargetField;
                            var joinAlias = $"ref_{originalFieldName}";
                            joinClauses.Add($"LEFT JOIN \"{targetTable}\" {joinAlias} ON main.\"{field.FieldNameDB}\" = {joinAlias}.\"Id\"");
                            selectFields.Add($"{joinAlias}.\"{targetField}\" AS \"{originalFieldName}\"");
                        }
                    }
                    else if (!field.FieldNameDB.EndsWith("Id") || !field.IsForeignKey)
                    {
                        selectFields.Add($"main.\"{field.FieldNameDB}\"");
                    }
                }
                if (!selectFields.Any(s => s.Contains("main.\"Id\"")))
                {
                    selectFields.Insert(0, "main.\"Id\"");
                }

                queryBuilder.Append($"SELECT {string.Join(", ", selectFields)} ");
                queryBuilder.Append($"FROM \"{metaTable.TableNameDB}\" main ");

                if (joinClauses.Any())
                {
                    queryBuilder.Append(string.Join(" ", joinClauses) + " ");
                }

                queryBuilder.Append("WHERE 1=1 ");

                if (!string.IsNullOrEmpty(input.Sorting))
                {
                    queryBuilder.Append($"ORDER BY {input.Sorting} ");
                }
                else
                {
                    queryBuilder.Append("ORDER BY main.\"Id\" ");
                }

                var offset = input.SkipCount;
                var limit = input.MaxResultCount > 0 ? input.MaxResultCount : 100;
                queryBuilder.Append($"LIMIT @limit OFFSET @offset");

                var query = queryBuilder.ToString();
                var parameters = new Dictionary<string, object>
                {
                    { "limit", limit },
                    { "offset", offset }
                };

                var results = await ExecuteQueryAsync(query, parameters);

                var items = new List<DataDto>();
                foreach (var row in results)
                {
                    var dataDto = new DataDto
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Data = new Dictionary<string, object>()
                    };

                    foreach (var kvp in row)
                    {
                        if (kvp.Key != "Id")
                        {
                            dataDto.Data[kvp.Key] = kvp.Value ?? string.Empty;
                        }
                    }

                    items.Add(dataDto);
                }

                var pageSize = input.MaxResultCount > 0 ? input.MaxResultCount : 100;
                var pageNumber = (input.SkipCount / pageSize) + 1;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return new PagedResultDto<DataDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasNextPage = pageNumber < totalPages,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi lấy dữ liệu!", ex);
            }
        }

        public async Task<List<DataDto>> GetDataByFieldAsync(string tableName, string fieldName)
        {
            try
            {
                var metaTable = await _tableRepository.FirstOrDefaultAsync(x => x.TableNameDB == tableName);
                if (metaTable == null)
                {
                    throw new UserFriendlyException($"Không tìm thấy bảng {tableName}!");
                }

                var metaFields = await _fieldRepository.GetAllListAsync(f => f.TableId == metaTable.Id);
                if (!metaFields.Any())
                {
                    throw new UserFriendlyException($"Bảng '{metaTable.DisplayNameVN}' không có field nào");
                }

                var targetField = metaFields.FirstOrDefault(f => f.FieldNameDB == fieldName);
                if (targetField == null)
                {
                    throw new UserFriendlyException($"Field '{fieldName}' không tồn tại trong bảng '{metaTable.DisplayNameVN}'!");
                }

                var query = $@"
                    SELECT ""Id"", ""{fieldName}""
                    FROM ""{tableName}"";";

                var result = new List<DataDto>();
                var connectionString = _configuration.GetConnectionString("Default");

                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = new NpgsqlCommand(query, connection))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var dto = new DataDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Data = new Dictionary<string, object>
                            {
                                [fieldName] = reader.IsDBNull(reader.GetOrdinal(fieldName))
                                    ? null
                                    : reader.GetValue(reader.GetOrdinal(fieldName))
                            }
                        };
                        result.Add(dto);
                    }
                }
                return result;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi lấy dữ liệu!", ex);
            }
        }
        public async Task<int> GetTotalRecordsCountAsync()
        {
            try
            {
                var allTables = await _tableRepository.GetAllListAsync();
                int totalCount = 0;

                foreach (var table in allTables)
                {
                    var query = $"SELECT COUNT(*) FROM \"{table.TableNameDB}\"";
                    var count = await ExecuteScalarAsync(query);
                    totalCount += count;
                }

                return totalCount;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi tính tổng số bản ghi!", ex);
            }
        }

        public async Task<int> GetTotalPublicCountAsync()
        {
            try
            {
                var publicTables = await _tableRepository.GetAllListAsync(x => x.IsPublic == true);
                int totalCount = 0;

                foreach (var table in publicTables)
                {
                    var query = $"SELECT COUNT(*) FROM \"{table.TableNameDB}\"";
                    var count = await ExecuteScalarAsync(query);
                    totalCount += count;
                }

                return totalCount;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi tính tổng số bản ghi!", ex);
            }
        }

        public async Task<List<TopTableDto>> GetTop5TablesAsync()
        {
            try
            {
                var allTables = await _tableRepository.GetAllListAsync();
                var topTables = new List<TopTableDto>();

                foreach (var table in allTables)
                {
                    var countQuery = $"SELECT COUNT(*) FROM \"{table.TableNameDB}\"";
                    var recordCount = await ExecuteScalarAsync(countQuery);

                    DateTime? lastDataModification = null;

                    var dataModQuery = $@"SELECT GREATEST(
                                            COALESCE(MAX(""CreationTime""), '1900-01-01'::timestamp),
                                            COALESCE(MAX(""LastModificationTime""), '1900-01-01'::timestamp)
                                        ) as last_data_mod
                                        FROM ""{table.TableNameDB}""";

                    var dataModResult = await ExecuteQueryAsync(dataModQuery);
                    if (dataModResult.Any() && dataModResult.First()["last_data_mod"] != null)
                    {
                        var dateValue = dataModResult.First()["last_data_mod"];
                        if (dateValue != DBNull.Value)
                        {
                            lastDataModification = Convert.ToDateTime(dateValue);
                        }
                    }

                    DateTime? lastTableStructureModification = table.LastModificationTime ?? table.CreationTime;

                    DateTime? finalLastModification = null;

                    if (lastDataModification.HasValue && lastTableStructureModification.HasValue)
                    {
                        finalLastModification = lastDataModification.Value > lastTableStructureModification.Value
                            ? lastDataModification.Value
                            : lastTableStructureModification.Value;
                    }
                    else if (lastDataModification.HasValue)
                    {
                        finalLastModification = lastDataModification.Value;
                    }
                    else if (lastTableStructureModification.HasValue)
                    {
                        finalLastModification = lastTableStructureModification.Value;
                    }

                    topTables.Add(new TopTableDto
                    {
                        TableName = table.TableNameDB,
                        DisplayName = table.DisplayNameVN,
                        RecordCount = recordCount,
                        LastModificationTime = finalLastModification,
                        IsPublic = table.IsPublic
                    });
                }

                var top5Tables = topTables
                    .OrderByDescending(t => t.RecordCount)
                    .Take(5)
                    .ToList();

                return top5Tables;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi lấy danh sách bảng!", ex);
            }
        }

        public async Task DeleteDataAsync([FromQuery] int tableId, [FromQuery] List<int> ids)
        {
            NpgsqlTransaction transaction = null;
            try
            {
                if (ids == null || !ids.Any())
                    throw new UserFriendlyException("Danh sách Ids không được để trống.");

                var metaTable = await _tableRepository.FirstOrDefaultAsync(x => x.Id == tableId);
                if (metaTable == null)
                {
                    throw new UserFriendlyException($"Không tìm thấy bảng với Id = {tableId}!");
                }

                var connectionString = _configuration.GetConnectionString("Default");
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                var sql = $@"DELETE FROM ""{metaTable.TableNameDB}"" 
                     WHERE ""Id"" = ANY(@ids);";

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@ids", ids);

                await command.ExecuteNonQueryAsync();

                await CurrentUnitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Abp.UI.UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi xóa dữ liệu!", ex);
            }
        }

        public async Task<DataGrowthSummaryDto> GetDataGrowthLast7DaysAsync()
        {
            try
            {
                var fromDate = DateTime.UtcNow.AddDays(-6).Date;
                var toDate = DateTime.UtcNow.Date;

                var allTables = await _tableRepository.GetAllListAsync();
                var dailyGrowthData = new List<DataGrowthDto>();

                foreach (var table in allTables)
                {
                    var query = $@" SELECT DATE(""CreationTime"") as creation_date,
                                        COUNT(*) as record_count
                                    FROM ""{table.TableNameDB}""
                                    WHERE ""CreationTime"" >= @fromDate 
                                        AND ""CreationTime"" <= @toDate + INTERVAL '23 hours 59 minutes 59 seconds'
                                    GROUP BY DATE(""CreationTime"")
                                    ORDER BY creation_date";

                    var parameters = new Dictionary<string, object>
                    {
                        { "fromDate", fromDate },
                        { "toDate", toDate }
                    };

                    var results = await ExecuteQueryAsync(query, parameters);

                    foreach (var result in results)
                    {
                        dailyGrowthData.Add(new DataGrowthDto
                        {
                            Date = Convert.ToDateTime(result["creation_date"]),
                            RecordCount = Convert.ToInt32(result["record_count"])
                        });
                    }
                }

                var summaryByDate = dailyGrowthData
                    .GroupBy(d => d.Date)
                    .Select(g => new DataGrowthDto
                    {
                        Date = g.Key,
                        RecordCount = g.Sum(x => x.RecordCount)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                var completeData = new List<DataGrowthDto>();
                for (int i = 0; i < 7; i++)
                {
                    var currentDate = fromDate.AddDays(i);
                    var existingData = summaryByDate.FirstOrDefault(x => x.Date.Date == currentDate);

                    completeData.Add(new DataGrowthDto
                    {
                        Date = currentDate,
                        RecordCount = existingData?.RecordCount ?? 0
                    });
                }

                return new DataGrowthSummaryDto
                {
                    DailyGrowth = completeData,
                    TotalRecordsLast7Days = completeData.Sum(x => x.RecordCount),
                    FromDate = fromDate,
                    ToDate = toDate
                };
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi lấy dữ liệu!", ex);
            }
        }

        #region Helper
        private async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            var result = new List<Dictionary<string, object>>();
            var connectionString = _configuration.GetConnectionString("Default");

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var fieldName = reader.GetName(i);
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[fieldName] = value;
                            }
                            result.Add(row);
                        }
                    }
                }
            }

            return result;
        }

        private async Task<int> ExecuteScalarAsync(string query, Dictionary<string, object> parameters = null)
        {
            var connectionString = _configuration.GetConnectionString("Default");

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        private async Task<DataValidationResultDto> ValidateDataAsync(Dictionary<string, object> data, List<MetaField> metaFields, string tableName)
        {
            var result = new DataValidationResultDto { IsValid = true };
            var auditFields = new[] { "CreationTime", "CreatorUserId", "LastModificationTime", "LastModifierUserId" };

            foreach (var field in metaFields)
            {
                if (auditFields.Contains(field.FieldNameDB)) continue;
                var fieldValue = data.ContainsKey(field.FieldNameDB) ? data[field.FieldNameDB] : null;

                if ((field.IsRequired && field.FieldNameDB != "Id") && (fieldValue == null || string.IsNullOrWhiteSpace(fieldValue?.ToString())))
                {
                    result.Errors.Add(new DataValidationErrorDto
                    {
                        FieldName = field.FieldNameDB,
                        ErrorMessage = $"Trường '{field.DisplayNameVN}' là bắt buộc",
                        ErrorType = "Required",
                        InvalidValue = fieldValue
                    });
                }

                if (fieldValue != null && !DatasHelper.IsValidDataType(fieldValue, field.DataType))
                {
                    result.Errors.Add(new DataValidationErrorDto
                    {
                        FieldName = field.FieldNameDB,
                        ErrorMessage = $"Trường '{field.DisplayNameVN}' phải có kiểu dữ liệu {field.DataType}",
                        ErrorType = "DataType",
                        InvalidValue = fieldValue
                    });
                }

                if (field.IsUnique && fieldValue != null)
                {
                    var isDuplicate = await CheckUniqueConstraintAsync(tableName, field.FieldNameDB, fieldValue);
                    if (isDuplicate)
                    {
                        result.Errors.Add(new DataValidationErrorDto
                        {
                            FieldName = field.FieldNameDB,
                            ErrorMessage = $"Giá trị '{fieldValue}' đã tồn tại cho trường '{field.DisplayNameVN}'",
                            ErrorType = "Unique",
                            InvalidValue = fieldValue
                        });
                    }
                }

                if (field.IsForeignKey && fieldValue != null)
                {
                    var relations = await _relationAppService.GetByTableAsync(tableName);
                    var relation = relations.FirstOrDefault(r =>
                                    r.SourceTable == tableName && r.SourceField == field.FieldNameDB);
                    var isValidForeignKey = await CheckForeignKeyConstraintAsync(field.TargetField, fieldValue, relation);
                    if (!isValidForeignKey)
                    {
                        result.Errors.Add(new DataValidationErrorDto
                        {
                            FieldName = field.FieldNameDB,
                            ErrorMessage = $"Giá trị '{fieldValue}' không tồn tại trong bảng tham chiếu",
                            ErrorType = "ForeignKey",
                            InvalidValue = fieldValue
                        });
                    }
                }
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        private async Task<bool> CheckUniqueConstraintAsync(string tableName, string fieldName, object value)
        {
            var query = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"{fieldName}\" = @value";
            var parameters = new Dictionary<string, object> { { "value", value } };

            var count = await ExecuteScalarAsync(query, parameters);
            return count > 0;
        }

        private async Task<bool> CheckForeignKeyConstraintAsync(string targetField, object value, RelationDto relation)
        {
            var targetTable = relation.TargetTable;
            var targetColumn = relation.TargetField;

            var query = $"SELECT COUNT(*) FROM \"{targetTable}\" WHERE \"{targetColumn}\" = @value";
            var parameters = new Dictionary<string, object> { { "value", value } };

            var count = await ExecuteScalarAsync(query, parameters);
            return count > 0;
        }

        private async Task<Dictionary<string, object>> PrepareInsertDataAsync(Dictionary<string, object> inputData, List<MetaField> metaFields, bool autoGenerateId)
        {
            var insertData = new Dictionary<string, object>();

            foreach (var field in metaFields)
            {
                var dataType = field.DataType.ToLower();
                if ((dataType == "int4" && field.FieldNameDB.EndsWith("Id")) && !inputData.ContainsKey(field.FieldNameDB))
                {
                    continue;
                }

                if (inputData.ContainsKey(field.FieldNameDB))
                {
                    var value = inputData[field.FieldNameDB];

                    if (value != null)
                    {
                        value = DatasHelper.ConvertToDataType(value.ToString(), field.DataType);
                    }
                    else if (!string.IsNullOrEmpty(field.DefaultValue))
                    {
                        value = DatasHelper.ConvertToDataType(field.DefaultValue, field.DataType);
                    }

                    insertData[field.FieldNameDB] = value;
                }
                else if (!string.IsNullOrEmpty(field.DefaultValue))
                {
                    insertData[field.FieldNameDB] = DatasHelper.ConvertToDataType(field.DefaultValue, field.DataType);
                }
            }
            insertData["CreationTime"] = DateTime.UtcNow;
            insertData["CreatorUserId"] = AbpSession.UserId;
            return insertData;
        }

        private async Task<int> ExecuteInsertAsync(string query, Dictionary<string, object> parameters)
        {
            var connectionString = _configuration.GetConnectionString("Default");

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using (var cmd = new NpgsqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        if (param.Key == "Id" && param.Key.EndsWith("Id")) { continue; }
                        
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null ? 0 : Convert.ToInt32(result);
                }
            }
        }

        private async Task<Dictionary<string, object>> GetCreatedDataAsync(string tableName, int id, List<MetaField> metaFields)
        {
            var selectFields = string.Join(", ", metaFields.Select(f => $"\"{f.FieldNameDB}\""));
            var query = $"SELECT {selectFields} FROM \"{tableName}\" WHERE \"Id\" = @id";
            var parameters = new Dictionary<string, object> { { "id", id } };

            var result = await ExecuteQueryAsync(query, parameters);
            return result.FirstOrDefault() ?? new Dictionary<string, object>();
        }

        private async Task<bool> CheckRecordExistsAsync(string tableName, int id)
        {
            var query = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"Id\" = @id";
            var parameters = new Dictionary<string, object> { { "id", id } };

            var count = await ExecuteScalarAsync(query, parameters);
            return count > 0;
        }

        private async Task<DataValidationResultDto> ValidateDataForUpdateAsync(Dictionary<string, object> data, List<MetaField> metaFields, string tableName, int recordId)
        {
            var result = new DataValidationResultDto { IsValid = true };
            var auditFields = new[] { "CreationTime", "CreatorUserId", "LastModificationTime", "LastModifierUserId" };
            foreach (var field in metaFields)
            {
                if (auditFields.Contains(field.FieldNameDB)) continue;
                var fieldValue = data.ContainsKey(field.FieldNameDB) ? data[field.FieldNameDB] : null;

                if (field.FieldNameDB == "Id") continue;

                if (field.IsRequired && data.ContainsKey(field.FieldNameDB) &&
                    (fieldValue == null || string.IsNullOrWhiteSpace(fieldValue?.ToString())))
                {
                    result.Errors.Add(new DataValidationErrorDto
                    {
                        FieldName = field.FieldNameDB,
                        ErrorMessage = $"Trường '{field.DisplayNameVN}' là bắt buộc",
                        ErrorType = "Required",
                        InvalidValue = fieldValue
                    });
                }

                if (fieldValue != null && !DatasHelper.IsValidDataType(fieldValue, field.DataType))
                {
                    result.Errors.Add(new DataValidationErrorDto
                    {
                        FieldName = field.FieldNameDB,
                        ErrorMessage = $"Trường '{field.DisplayNameVN}' phải có kiểu dữ liệu {field.DataType}",
                        ErrorType = "DataType",
                        InvalidValue = fieldValue
                    });
                }

                if (field.IsUnique && fieldValue != null)
                {
                    var isDuplicate = await CheckUniqueConstraintForUpdateAsync(tableName, field.FieldNameDB, fieldValue, recordId);
                    if (isDuplicate)
                    {
                        result.Errors.Add(new DataValidationErrorDto
                        {
                            FieldName = field.FieldNameDB,
                            ErrorMessage = $"Giá trị '{fieldValue}' đã tồn tại cho trường '{field.DisplayNameVN}'",
                            ErrorType = "Unique",
                            InvalidValue = fieldValue
                        });
                    }
                }

                if (field.IsForeignKey && fieldValue != null)
                {
                    var relations = await _relationAppService.GetByTableAsync(tableName);
                    var relation = relations.FirstOrDefault(r =>
                                    r.SourceTable == tableName && r.SourceField == field.FieldNameDB);
                    if (relation != null)
                    {
                        var isValidForeignKey = await CheckForeignKeyConstraintAsync(field.TargetField, fieldValue, relation);
                        if (!isValidForeignKey)
                        {
                            result.Errors.Add(new DataValidationErrorDto
                            {
                                FieldName = field.FieldNameDB,
                                ErrorMessage = $"Giá trị '{fieldValue}' không tồn tại trong bảng tham chiếu",
                                ErrorType = "ForeignKey",
                                InvalidValue = fieldValue
                            });
                        }
                    }
                }
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        private async Task<bool> CheckUniqueConstraintForUpdateAsync(string tableName, string fieldName, object value, int recordId)
        {
            var query = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"{fieldName}\" = @value AND \"Id\" != @id";
            var parameters = new Dictionary<string, object>
            {
                { "value", value },
                { "id", recordId }
            };

            var count = await ExecuteScalarAsync(query, parameters);
            return count > 0;
        }

        private async Task<Dictionary<string, object>> PrepareUpdateDataAsync(Dictionary<string, object> inputData, List<MetaField> metaFields)
        {
            var updateData = new Dictionary<string, object>();

            foreach (var field in metaFields)
            {
                if (field.FieldNameDB == "Id") continue;

                if (inputData.ContainsKey(field.FieldNameDB))
                {
                    var value = inputData[field.FieldNameDB];

                    if (value != null)
                    {
                        value = DatasHelper.ConvertToDataType(value.ToString(), field.DataType);
                    }

                    updateData[field.FieldNameDB] = value;
                }
            }
            updateData["LastModificationTime"] = DateTime.UtcNow;
            updateData["LastModifierUserId"] = AbpSession.UserId;
            return updateData;
        }

        private string BuildUpdateQuery(string tableName, Dictionary<string, object> data, int id)
        {
            return DatasHelper.BuildUpdateQuery(tableName, data);
        }

        private async Task ExecuteUpdateAsync(string query, Dictionary<string, object> parameters, int id)
        {
            var connectionString = _configuration.GetConnectionString("Default");

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("Id", id);

                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }


        #endregion
    }
}
