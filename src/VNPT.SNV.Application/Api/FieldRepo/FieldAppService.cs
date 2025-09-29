using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
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
using VNPT.SNV.Api.FieldRepo.Dtos;
using VNPT.SNV.EntityFrameworkCore;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.FieldRepo
{
    [Authorize]
    public class FieldAppService : CrudAppService<
        MetaField,
        FieldDto,
        int,
        PagedAndSortedResultRequestDto,
        CreateFieldDto,
        UpdateFieldDto>, IFieldAppService
    {
        private readonly IRepository<MetaField, int> _fieldRepository;
        private readonly IRepository<MetaTable, int> _tableRepository;
        private readonly IConfiguration _configuration;

        public FieldAppService(IRepository<MetaField, int> repository, IRepository<MetaTable, int> repository1, IConfiguration configuration) : base(repository)
        {
            _fieldRepository = repository;
            _tableRepository = repository1;
            _configuration = configuration;
        }

        public async Task<List<FieldDto>> CreateFieldAsync(List<CreateFieldDto> inputs)
        {
            using (var unitOfWork = UnitOfWorkManager.Begin())
            {
                NpgsqlTransaction transaction = null;
                try
                {
                    if (inputs == null || inputs.Count == 0)
                        throw new UserFriendlyException("Danh sách field không được để trống.");

                    foreach (var group in inputs.GroupBy(x => new { x.TableId, x.FieldNameDB }))
                    {
                        if (group.Count() > 1)
                            throw new UserFriendlyException($"Tên field '{group.Key.FieldNameDB}' bị trùng trong danh sách.");
                    }

                    foreach (var group in inputs.GroupBy(x => new { x.TableId, x.DisplayNameVN }))
                    {
                        if (group.Count() > 1)
                            throw new UserFriendlyException($"Tên hiển thị field '{group.Key.DisplayNameVN}' bị trùng trong danh sách.");
                    }

                    var tableIds = inputs.Select(x => x.TableId).Distinct().ToList();

                    var existingFields = await _fieldRepository.GetAllListAsync(f => tableIds.Contains(f.TableId));

                    foreach (var input in inputs)
                    {
                        if (existingFields.Any(f => f.TableId == input.TableId && f.FieldNameDB == input.FieldNameDB))
                            throw new UserFriendlyException($"Tên field '{input.FieldNameDB}' đã tồn tại trong bảng.");

                        if (existingFields.Any(f => f.TableId == input.TableId && f.DisplayNameVN == input.DisplayNameVN))
                            throw new UserFriendlyException($"Tên hiển thị field '{input.DisplayNameVN}' đã tồn tại trong bảng.");
                    }

                    var entities = ObjectMapper.Map<List<MetaField>>(inputs);
                    var connectionString = _configuration.GetConnectionString("Default");
                    await using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    transaction = await connection.BeginTransactionAsync();

                    foreach (var entity in entities)
                    {
                        await _fieldRepository.InsertAsync(entity);
                        var tableName = await _tableRepository.FirstOrDefaultAsync(entity.TableId);
                        var sql = FieldHelper.AddFieldSql(tableName.TableNameDB, entity);
                        
                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        await command.ExecuteNonQueryAsync();
                    }
                    await CurrentUnitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await unitOfWork.CompleteAsync();

                    return ObjectMapper.Map<List<FieldDto>>(entities);
                }
                catch (Abp.UI.UserFriendlyException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi tạo field!", ex);
                }
            }
        }

        public async Task DeleteFieldAsync([FromQuery] List<int> ids)
        {
            NpgsqlTransaction transaction = null;
            try
            {
                if (ids == null || !ids.Any())
                    throw new UserFriendlyException("Danh sách Ids không được để trống.");

                var connectionString = _configuration.GetConnectionString("Default");
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();
                foreach (var id in ids)
                {
                    var metaField = await _fieldRepository.FirstOrDefaultAsync(id);
                    if (metaField == null)
                    {
                        throw new Abp.UI.UserFriendlyException($"Không tìm thấy field với Id = {id}!");
                    }
                    var metaTable = await _tableRepository.FirstOrDefaultAsync(metaField.TableId);
                    if (metaTable == null)
                    {
                        throw new Abp.UI.UserFriendlyException($"Không tìm thấy bảng với Id = {metaField.TableId}!");
                    }
                    await _fieldRepository.DeleteAsync(id);
                    var sql = FieldHelper.DeleteFieldSql(metaTable.TableNameDB, metaField);
                    await using var command = new NpgsqlCommand(sql, connection, transaction);
                    await command.ExecuteNonQueryAsync();
                }

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
                throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi xóa field!", ex);
            }
        }

        public async Task<List<FieldDto>> GetByTableIdAsync(int tableId)
        {
            try
            {
                var metaTable = await _tableRepository.FirstOrDefaultAsync(tableId);
                if (metaTable == null)
                {
                    throw new Abp.UI.UserFriendlyException($"Không tìm thấy bảng với Id = {tableId}!");
                }
                var query = await _fieldRepository.GetAllIncluding(x => x.metaTable)
                    .Where(x => x.TableId == tableId)
                    .ToListAsync();

                return ObjectMapper.Map<List<FieldDto>>(query);
            }
            catch (Abp.UI.UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi lấy danh sách field!", ex);
            }
        }

        public async Task<List<FieldDto>> UpdateFieldAsync(List<UpdateFieldDto> inputs)
        {
            using (var unitOfWork = UnitOfWorkManager.Begin())
            {
                NpgsqlTransaction transaction = null;
                try
                {
                    if (inputs == null || inputs.Count == 0)
                        throw new UserFriendlyException("Danh sách field không được để trống.");

                    foreach (var group in inputs.GroupBy(x => new { x.TableId, x.FieldNameDB }))
                    {
                        if (group.Count() > 1)
                            throw new UserFriendlyException($"Tên field '{group.Key.FieldNameDB}' bị trùng trong danh sách.");
                    }

                    foreach (var group in inputs.GroupBy(x => new { x.TableId, x.DisplayNameVN }))
                    {
                        if (group.Count() > 1)
                            throw new UserFriendlyException($"Tên hiển thị field '{group.Key.DisplayNameVN}' bị trùng trong danh sách.");
                    }

                    var ids = inputs.Select(x => x.Id).ToList();
                    var tableIds = inputs.Select(x => x.TableId).Distinct().ToList();

                    var existingFields = await _fieldRepository.GetAllListAsync(f => tableIds.Contains(f.TableId));
                    foreach (var input in inputs)
                    {
                        var metaTable = await _tableRepository.FirstOrDefaultAsync(input.TableId);
                        if (metaTable == null)
                        {
                            throw new Abp.UI.UserFriendlyException($"Không tìm thấy bảng với Id = {input.TableId}!");
                        }
                        var entity = existingFields.FirstOrDefault(f => f.Id == input.Id);
                        if (entity == null)
                            throw new UserFriendlyException($"Không tìm thấy field với Id={input.Id}");

                        if (existingFields.Any(f => f.TableId == input.TableId && f.FieldNameDB == input.FieldNameDB && f.Id != input.Id))
                            throw new UserFriendlyException($"Tên field '{input.FieldNameDB}' đã tồn tại trong bảng.");

                        if (existingFields.Any(f => f.TableId == input.TableId && f.DisplayNameVN == input.DisplayNameVN && f.Id != input.Id))
                            throw new UserFriendlyException($"Tên hiển thị field '{input.DisplayNameVN}' đã tồn tại trong bảng.");
                    }

                    var updatedEntities = new List<MetaField>();
                    var connectionString = _configuration.GetConnectionString("Default");
                    await using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    transaction = await connection.BeginTransactionAsync();
                    foreach (var input in inputs)
                    {
                        var metaTable = await _tableRepository.FirstOrDefaultAsync(input.TableId);

                        var entity = existingFields.First(f => f.Id == input.Id);

                        var oldField = new MetaField
                        {
                            Id = entity.Id,
                            TableId = entity.TableId,
                            FieldNameDB = entity.FieldNameDB,
                            DisplayNameVN = entity.DisplayNameVN,
                            DataType = entity.DataType,
                            IsRequired = entity.IsRequired,
                            IsUnique = entity.IsUnique,
                            DefaultValue = entity.DefaultValue
                        };

                        ObjectMapper.Map(input, entity);

                        var sqlList = FieldHelper.UpdateFieldSql(
                            tableName: metaTable.TableNameDB,
                            oldField: oldField,
                            newField: entity
                        );

                        foreach (var sql in sqlList)
                        {
                            await using var command = new NpgsqlCommand(sql, connection, transaction);
                            await command.ExecuteNonQueryAsync();
                        }

                        await _fieldRepository.UpdateAsync(entity);
                        updatedEntities.Add(entity);
                    }

                    await CurrentUnitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await unitOfWork.CompleteAsync();

                    return ObjectMapper.Map<List<FieldDto>>(updatedEntities);
                }
                catch (Abp.UI.UserFriendlyException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Abp.UI.UserFriendlyException("Có lỗi xảy ra khi cập nhật field!" + ex);
                }
            }
        }
    }
}
