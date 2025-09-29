using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNPT.SNV.Api.SearchRepo.Dtos;
using VNPT.SNV.Models;

namespace VNPT.SNV.Api.SearchRepo
{
    public class SearchAppService : ApplicationService, ISearchAppService
    {
        private readonly IRepository<MetaTable, int> _tableRepository;
        private readonly IRepository<MetaField, int> _fieldRepository;
        private readonly IConfiguration _configuration;

        public SearchAppService(
            IRepository<MetaTable, int> tableRepository,
            IRepository<MetaField, int> fieldRepository,
            IConfiguration configuration)
        {
            _tableRepository = tableRepository;
            _fieldRepository = fieldRepository;
            _configuration = configuration;

        }

        public async Task<SearchResultDto> SearchAsync(SearchInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.SearchText))
                    throw new UserFriendlyException("Từ khóa tìm kiếm không được để trống!");

                if (input.SearchText.Length < 2)
                    throw new UserFriendlyException("Từ khóa tìm kiếm phải có ít nhất 2 ký tự!");

                if (input.SearchText.Length > 100)
                    throw new UserFriendlyException("Từ khóa tìm kiếm không được vượt quá 100 ký tự!");

                var sanitizedText = SearchHelper.SanitizeSearchText(input.SearchText);

                var publicTables = await _tableRepository.GetAllListAsync(t => t.IsPublic == true);
                if (!publicTables.Any())
                    throw new UserFriendlyException("Không có bảng công khai để tìm kiếm!");

                var searchType = SearchHelper.DetermineSearchType(sanitizedText);

                var searchResults = new List<TableSearchResultDto>();
                var totalResults = 0;

                foreach (var table in publicTables)
                {
                    var tableResult = await SearchInTableAsync(table, sanitizedText, searchType, input.MaxResultsPerTable);
                    if (tableResult.Results.Any())
                    {
                        searchResults.Add(tableResult);
                        totalResults += tableResult.TotalCount;
                    }
                }

                if (input.MaxTotalResults > 0)
                {
                    var currentCount = 0;
                    var limitedResults = new List<TableSearchResultDto>();

                    foreach (var tableResult in searchResults)
                    {
                        var remainingSlots = input.MaxTotalResults - currentCount;
                        if (remainingSlots <= 0) break;

                        if (tableResult.Results.Count > remainingSlots)
                        {
                            tableResult.Results = tableResult.Results.Take(remainingSlots).ToList();
                            tableResult.TotalCount = tableResult.Results.Count;
                        }

                        limitedResults.Add(tableResult);
                        currentCount += tableResult.Results.Count;
                    }

                    searchResults = limitedResults;
                }

                return new SearchResultDto
                {
                    SearchText = input.SearchText,
                    SearchType = searchType,
                    TotalTables = searchResults.Count,
                    TotalResults = searchResults.Sum(r => r.Results.Count),
                    TableResults = searchResults,
                    SearchTime = DateTime.UtcNow,
                    Message = searchResults.Any() ? "Tìm kiếm thành công!" : "Không tìm thấy kết quả nào!"
                };
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("Có lỗi xảy ra khi tìm kiếm!", ex);
            }
        }


        #region
        private async Task<TableSearchResultDto> SearchInTableAsync(MetaTable table, string searchText, SearchType searchType, int maxResults)
        {
            var fields = await _fieldRepository.GetAllListAsync(f => f.TableId == table.Id);
            var searchableFields = SearchHelper.GetSearchableFields(fields, searchType);

            if (!searchableFields.Any())
            {
                return new TableSearchResultDto
                {
                    TableName = table.TableNameDB,
                    DisplayName = table.DisplayNameVN,
                    Results = new List<Dictionary<string, object>>(),
                    TotalCount = 0
                };
            }

            var query = SearchHelper.BuildSearchQuery(table, fields, searchableFields, searchText, searchType, maxResults);
            var countQuery = SearchHelper.BuildCountQuery(table, searchableFields, searchText, searchType);

            var results = await ExecuteQueryAsync(query.Query, query.Parameters, fields);
            var totalCount = await ExecuteScalarAsync(countQuery.Query, countQuery.Parameters);

            return new TableSearchResultDto
            {
                TableName = table.TableNameDB,
                DisplayName = table.DisplayNameVN,
                Results = results,
                TotalCount = totalCount
            };
        }

        private async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, Dictionary<string, object> parameters, List<MetaField> fields)
        {
            var result = new List<Dictionary<string, object>>();
            var connectionString = _configuration.GetConnectionString("Default");

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using (var cmd = new NpgsqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }

                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var fieldName = reader.GetName(i);
                                var fieldNameVN = fields.FirstOrDefault(f => f.FieldNameDB == fieldName);
                                fieldName = fieldNameVN.DisplayNameVN;
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

        private async Task<int> ExecuteScalarAsync(string query, Dictionary<string, object> parameters)
        {
            var connectionString = _configuration.GetConnectionString("Default");

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using (var cmd = new NpgsqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }

                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }
        #endregion
    }
}

