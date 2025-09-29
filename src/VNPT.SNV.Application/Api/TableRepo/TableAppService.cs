using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.FieldRepo;
using VNPT.SNV.Api.FieldRepo.Dtos;
using VNPT.SNV.Api.TableRepo.Dtos;
using VNPT.SNV.EntityFrameworkCore;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.TableRepo
{
    [Authorize]
    public class TableAppService : CrudAppService<
        MetaTable,
        TableDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateTableDto,
        UpdateTableDto>, ITableAppService
    {
        private readonly IRepository<MetaTable, int> _tableRepository;
        private readonly IConfiguration _configuration;
        private readonly IRepository<MetaField, int> _fieldRepository;

        public TableAppService(IRepository<MetaTable, int> repository, IConfiguration configuration, IRepository<MetaField, int> repository1) : base(repository)
        {
            _tableRepository = repository;
            _configuration = configuration;
            _fieldRepository = repository1;
        }

        public async Task<TableDto> CreateTableAsync(CreateTableDto input)
        {
            using (var unitOfWork = UnitOfWorkManager.Begin())
            {
                NpgsqlTransaction transaction = null;
                try
                {
                    var existsByTableName = await _tableRepository.FirstOrDefaultAsync(x => x.TableNameDB == input.TableNameDB);
                    if (existsByTableName != null)
                    {
                        throw new UserFriendlyException($"Tên bảng '{input.TableNameDB}' đã tồn tại!");
                    }
                    var existsByDisplayName = await _tableRepository.FirstOrDefaultAsync(x => x.DisplayNameVN == input.DisplayNameVN);
                    if (existsByDisplayName != null)
                    {
                        throw new UserFriendlyException($"Tên hiển thị '{input.DisplayNameVN}' đã tồn tại!");
                    }

                    ApiHelper.ValidateIdentifier(input.TableNameDB, "Tên bảng không hợp lệ!", "Định dạng tên bảng không hợp lệ!");

                    var entity = ObjectMapper.Map<MetaTable>(input);

                    await _tableRepository.InsertAsync(entity);

                    await CurrentUnitOfWork.SaveChangesAsync();

                    var sql = $@"
                        CREATE TABLE IF NOT EXISTS ""{input.TableNameDB}"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""CreationTime"" TIMESTAMP,
                            ""CreatorUserId"" INT4,
                            ""LastModificationTime"" TIMESTAMP,
                            ""LastModifierUserId"" INT4
                        );";
                    var connectionString = _configuration.GetConnectionString("Default");
                    await using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    transaction = await connection.BeginTransactionAsync();
                    await using var command = new NpgsqlCommand(sql, connection, transaction);
                    await command.ExecuteNonQueryAsync();

                    var createFieldDto = new CreateFieldDto
                    {
                        FieldNameDB = "Id",
                        DisplayNameVN = "ID",
                        DataType = "int4",
                        IsRequired = true,
                        IsUnique = true,
                        DefaultValue = "SERIAL (KHÓA CHÍNH)",
                        TableId = entity.Id,
                    };

                    var metaFields = ObjectMapper.Map<MetaField>(createFieldDto);
                    await _fieldRepository.InsertAsync(metaFields);

                    await CurrentUnitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await unitOfWork.CompleteAsync();

                    return ObjectMapper.Map<TableDto>(entity);
                }
                catch (Abp.UI.UserFriendlyException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi tạo bảng!", ex);
                }
            }
        }

        public async Task DeleteTableAsync(int id)
        {
            NpgsqlTransaction transaction = null;
            try
            {
                var metaTable = await _tableRepository.FirstOrDefaultAsync(id);
                if (metaTable == null)
                {
                    throw new Abp.UI.UserFriendlyException($"Không tìm thấy bảng với Id = {id}!");
                }

                await _tableRepository.DeleteAsync(id);
                var sql = $@"DROP TABLE IF EXISTS ""{metaTable.TableNameDB}"";";
                var connectionString = _configuration.GetConnectionString("Default");
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();
                await using var command = new NpgsqlCommand(sql, connection, transaction);
                await command.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch (Abp.UI.UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi xóa bảng!", ex);
            }
        }

        public async Task<TableDto> UpdateTableAsync(UpdateTableDto input)
        {
            using (var unitOfWork = UnitOfWorkManager.Begin())
            {
                NpgsqlTransaction transaction = null;
                try
                {
                    if (input.TableNameDB == null || input.DisplayNameVN == null)
                    {
                        throw new Abp.UI.UserFriendlyException($"Không được để trống tên/tên hiển thị bảng.");
                    }
                    var metaTable = await _tableRepository.GetAsync(input.Id);
                    if (metaTable == null)
                    {
                        throw new Abp.UI.UserFriendlyException($"Không tìm thấy bảng với Id = {input.Id}!");
                    }
                    var existingByTableName = await _tableRepository.FirstOrDefaultAsync(
                        x => x.TableNameDB == input.TableNameDB && x.Id != input.Id
                    );
                    if (existingByTableName != null)
                    {
                        throw new Abp.UI.UserFriendlyException($"Tên bảng '{input.TableNameDB}' đã tồn tại!");
                    }
                    var existingByDisplayName = await _tableRepository.FirstOrDefaultAsync(
                        x => x.DisplayNameVN == input.DisplayNameVN && x.Id != input.Id
                    );
                    if (existingByDisplayName != null)
                    {
                        throw new Abp.UI.UserFriendlyException($"Tên hiển thị bảng '{input.DisplayNameVN}' đã tồn tại!");
                    }
                    ApiHelper.ValidateIdentifier(input.TableNameDB, "Tên bảng không hợp lệ!", "Định dạng tên bảng không hợp lệ!");

                    var oldTableName = metaTable.TableNameDB;

                    ObjectMapper.Map(input, metaTable);

                    await _tableRepository.UpdateAsync(metaTable);

                    var connectionString = _configuration.GetConnectionString("Default");
                    await using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    transaction = await connection.BeginTransactionAsync();

                    if (oldTableName != input.TableNameDB)
                    {
                        var sql = $@"ALTER TABLE ""{oldTableName}"" RENAME TO ""{input.TableNameDB}"";";
                        
                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    await unitOfWork.CompleteAsync();
                    await transaction.CommitAsync();
                    return ObjectMapper.Map<TableDto>(metaTable);
                }
                catch (Abp.UI.UserFriendlyException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi cập nhật bảng!", ex);
                }
            }
        }
    }
}
