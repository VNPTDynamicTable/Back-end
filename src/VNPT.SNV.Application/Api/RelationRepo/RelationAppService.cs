using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.RelationRepo.Dtos;
using VNPT.SNV.EntityFrameworkCore;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.RelationRepo
{
    [Authorize]
    public class RelationAppService : ApplicationService, IRelationAppService
    {
        private readonly IRepository<MetaTable, int> _tableRepository;
        private readonly IRepository<MetaField, int> _fieldRepository;
        private readonly IConfiguration _configuration;

        public RelationAppService(IRepository<MetaTable, int> tableRepository, IRepository<MetaField, int> fieldRepository, IConfiguration configuration)
        {
            _tableRepository = tableRepository;
            _fieldRepository = fieldRepository;
            _configuration = configuration;
        }

        public async Task<List<RelationDto>> CreateRelationAsync(List<CreateRelationDto> inputs)
        {
            using (var unitOfWork = UnitOfWorkManager.Begin())
            {
                var results = new List<RelationDto>();

                foreach (var input in inputs)
                {
                    try
                    {
                        var existsByTable = await _tableRepository.FirstOrDefaultAsync(x => x.TableNameDB == input.SourceTable);
                        if (existsByTable == null)
                        {
                            throw new UserFriendlyException($"Bảng '{input.SourceTable}' không tồn tại!");
                        }

                        var existsByTable1 = await _tableRepository.FirstOrDefaultAsync(x => x.TableNameDB == input.TargetTable);
                        if (existsByTable1 == null)
                        {
                            throw new UserFriendlyException($"Bảng '{input.TargetTable}' không tồn tại!");
                        }

                        var existsByField = await _fieldRepository.FirstOrDefaultAsync(x => x.FieldNameDB == input.SourceField && x.TableId == existsByTable.Id);
                        if (existsByField == null)
                        {
                            throw new UserFriendlyException($"Field '{input.SourceField}' không tồn tại!");
                        }

                        var existsByField1 = await _fieldRepository.FirstOrDefaultAsync(x => x.FieldNameDB == input.TargetField && x.TableId == existsByTable1.Id);
                        if (existsByField1 == null)
                        {
                            throw new UserFriendlyException($"Field '{input.TargetField}' không tồn tại!");
                        }

                        if (string.IsNullOrWhiteSpace(input.ConstraintName))
                        {
                            input.ConstraintName =
                                $"{input.SourceTable}_{input.SourceField}_fkey";
                        }

                        var checkSql = @"
                            SELECT COUNT(*)
                            FROM information_schema.table_constraints tc
                            WHERE tc.constraint_type = 'FOREIGN KEY'
                              AND tc.table_name = @tableName
                              AND tc.constraint_name = @constraintName;";

                        var connectionString = _configuration.GetConnectionString("Default");
                        using (var connection = new NpgsqlConnection(connectionString))
                        {
                            await connection.OpenAsync();

                            await using (var cmd = new NpgsqlCommand(checkSql, connection))
                            {
                                cmd.Parameters.AddWithValue("tableName", input.SourceTable);
                                cmd.Parameters.AddWithValue("constraintName", input.ConstraintName);

                                var count = (long)(await cmd.ExecuteScalarAsync());

                                if (count > 0)
                                {
                                    throw new UserFriendlyException(
                                        $"Quan hệ {input.ConstraintName} đã tồn tại trên bảng {input.SourceTable}");
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(input.TypeDelete))
                            input.TypeDelete = "NO ACTION";
                        if (string.IsNullOrEmpty(input.TypeUpdate))
                            input.TypeUpdate = "NO ACTION";

                        var sql = $@"
                            ALTER TABLE ""{input.SourceTable}"" 
                            ADD CONSTRAINT ""{input.ConstraintName}"" 
                            FOREIGN KEY (""{input.SourceField}"") 
                            REFERENCES ""{input.TargetTable}"" (""{input.TargetField}"")
                            ON UPDATE {input.TypeUpdate}
                            ON DELETE {input.TypeDelete};";

                        using (var connection = new NpgsqlConnection(connectionString))
                        {
                            await connection.OpenAsync();

                            using (var command = new NpgsqlCommand(sql, connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        if (!existsByField.IsForeignKey)
                        {
                            existsByField.IsForeignKey = true;
                            await _fieldRepository.UpdateAsync(existsByField);
                        }

                        results.Add(new RelationDto
                        {
                            SourceTable = input.SourceTable,
                            TargetTable = input.TargetTable,
                            SourceField = input.SourceField,
                            TargetField = input.TargetField,
                            ConstraintName = input.ConstraintName,
                            TypeUpdate = input.TypeUpdate,
                            TypeDelete = input.TypeDelete
                        });
                    }
                    catch (Abp.UI.UserFriendlyException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new UserFriendlyException("Không thể tạo quan hệ. Chi tiết: " + ex.Message);
                    }
                }
                await unitOfWork.CompleteAsync();

                return results;
            }
        }

        public async Task DeleteRelationAsync([FromBody] List<CreateRelationDto> inputs)
        {
            foreach (var input in inputs)
            {
                using (var unitOfWork = UnitOfWorkManager.Begin())
                {
                    try
                    {
                        if (inputs == null || !inputs.Any())
                        {
                            throw new UserFriendlyException("Danh sách quan hệ cần xóa trống!");
                        }
                        var existsByTable = await _tableRepository.FirstOrDefaultAsync(x => x.TableNameDB == input.SourceTable);
                        if (existsByTable == null)
                        {
                            throw new UserFriendlyException($"Bảng '{input.SourceTable}' không tồn tại!");
                        }

                        var checkSql = @"
                            SELECT COUNT(*)
                            FROM information_schema.table_constraints tc
                            WHERE tc.constraint_type = 'FOREIGN KEY'
                              AND tc.table_name = @tableName
                              AND tc.constraint_name = @constraintName;";

                        var connectionString = _configuration.GetConnectionString("Default");

                        await using (var connection = new NpgsqlConnection(connectionString))
                        {
                            await connection.OpenAsync();

                            await using (var cmd = new NpgsqlCommand(checkSql, connection))
                            {
                                cmd.Parameters.AddWithValue("tableName", input.SourceTable);
                                cmd.Parameters.AddWithValue("constraintName", input.ConstraintName);

                                var count = (long)(await cmd.ExecuteScalarAsync());

                                if (count == 0)
                                {
                                    throw new UserFriendlyException(
                                        $"Quan hệ {input.ConstraintName} không tồn tại trên bảng {input.SourceTable}");
                                }
                            }
                        }

                        var sql = $@"ALTER TABLE ""{input.SourceTable}"" DROP CONSTRAINT ""{input.ConstraintName}"";";
                        using (var connection = new NpgsqlConnection(connectionString))
                        {
                            await connection.OpenAsync();

                            using (var command = new NpgsqlCommand(sql, connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        var existsByField = await _fieldRepository.FirstOrDefaultAsync(x => x.FieldNameDB == input.SourceField && x.TableId == existsByTable.Id);
                        if (existsByField != null && existsByField.IsForeignKey)
                        {
                            existsByField.IsForeignKey = false;
                            await _fieldRepository.UpdateAsync(existsByField);
                        }

                        await unitOfWork.CompleteAsync();
                    }
                    catch (UserFriendlyException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new UserFriendlyException("Không thể xóa quan hệ. Chi tiết: " + ex.Message);
                    }
                }
            }
        }

        public async Task<List<RelationDto>> GetByTableAsync(string tableName)
        {
            try
            {
                var existsByTable = await _tableRepository.FirstOrDefaultAsync(x => x.TableNameDB == tableName);
                if (existsByTable == null)
                {
                    throw new UserFriendlyException($"Bảng '{tableName}' không tồn tại!");
                }

                var sql = @"
                    SELECT
                        tc.constraint_name,
                        tc.table_name AS source_table,
                        kcu.column_name AS source_column,
                        ccu.table_name AS target_table,
                        ccu.column_name AS target_column,
                        rc.update_rule,
                        rc.delete_rule
                    FROM information_schema.table_constraints AS tc
                    JOIN information_schema.key_column_usage AS kcu
                        ON tc.constraint_name = kcu.constraint_name
                        AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage AS ccu
                        ON ccu.constraint_name = tc.constraint_name
                        AND ccu.table_schema = tc.table_schema
                    JOIN information_schema.referential_constraints AS rc
                        ON rc.constraint_name = tc.constraint_name
                        AND rc.constraint_schema = tc.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                      AND tc.table_name = @tableName;";


                var connectionString = _configuration.GetConnectionString("Default");

                var result = new List<RelationDto>();

                await using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    await using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("tableName", tableName);

                        await using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new RelationDto
                                {
                                    ConstraintName = reader.GetString(0),
                                    SourceTable = reader.GetString(1),
                                    SourceField = reader.GetString(2),
                                    TargetTable = reader.GetString(3),
                                    TargetField = reader.GetString(4),
                                    TypeUpdate = reader.GetString(5),
                                    TypeDelete = reader.GetString(6)
                                });
                            }
                        }
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
                throw new UserFriendlyException("Không thể lấy quan hệ của bảng. Chi tiết: " + ex.Message);
            }
        }
    }
}
