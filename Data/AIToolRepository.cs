using Itarix.Api.Models;
using itarixapi.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace itarixapi.Data
{
    public interface IAIToolRepository
    {
        Task<List<AIToolDto>> GetAllAsync();
        Task<AIToolDto> GetByIdAsync(int toolId);
        Task<AIToolDto> CreateAsync(AIToolCreateDto dto);
        Task<AIToolDto> EditAsync(int toolId, AIToolCreateDto dto);
        Task<bool> DeleteAsync(int toolId);
    }

    public class AIToolRepository : IAIToolRepository
    {
        private readonly string _connectionString;

        public AIToolRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<AIToolDto>> GetAllAsync()
        {
            var tools = new List<AIToolDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"SELECT t.ToolId, t.Name, t.Description, t.CategoryId, c.CategoryName, t.WebsiteURL, t.CreatedAt
                  FROM AITools t
                  INNER JOIN Categories c ON t.CategoryId = c.CategoryId", conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tools.Add(new AIToolDto
                        {
                            ToolId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CategoryId = reader.GetInt32(3),
                            CategoryName = reader.IsDBNull(4) ? null : reader.GetString(4),
                            WebsiteURL = reader.IsDBNull(5) ? null : reader.GetString(5),
                            CreatedAt = reader.GetDateTime(6)
                        });
                    }
                }
            }
            return tools;
        }

        public async Task<AIToolDto> GetByIdAsync(int toolId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"SELECT t.ToolId, t.Name, t.Description, t.CategoryId, c.CategoryName, t.WebsiteURL, t.CreatedAt
                  FROM AITools t
                  INNER JOIN Categories c ON t.CategoryId = c.CategoryId
                  WHERE t.ToolId = @ToolId", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", toolId);
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new AIToolDto
                        {
                            ToolId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CategoryId = reader.GetInt32(3),
                            CategoryName = reader.IsDBNull(4) ? null : reader.GetString(4),
                            WebsiteURL = reader.IsDBNull(5) ? null : reader.GetString(5),
                            CreatedAt = reader.GetDateTime(6)
                        };
                    }
                }
            }
            return null;
        }

        public async Task<AIToolDto> CreateAsync(AIToolCreateDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"INSERT INTO AITools (Name, Description, CategoryId, WebsiteURL, CreatedAt) 
          OUTPUT INSERTED.ToolId, INSERTED.Name, INSERTED.Description, INSERTED.CategoryId, INSERTED.WebsiteURL, INSERTED.CreatedAt
          VALUES (@Name, @Description, @CategoryId, @WebsiteURL, GETDATE())", conn))
            {
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoryId", dto.CategoryId);
                cmd.Parameters.AddWithValue("@WebsiteURL", (object)dto.WebsiteURL ?? DBNull.Value);

                await conn.OpenAsync();

                int toolId = 0, categoryId = 0;
                string name = "", description = null, websiteUrl = null;
                DateTime createdAt = DateTime.UtcNow;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        toolId = reader.GetInt32(0);
                        name = reader.GetString(1);
                        description = reader.IsDBNull(2) ? null : reader.GetString(2);
                        categoryId = reader.GetInt32(3);
                        websiteUrl = reader.IsDBNull(4) ? null : reader.GetString(4);
                        createdAt = reader.GetDateTime(5);
                    }
                }

                // Now safe to query again
                var categoryName = await GetCategoryNameByIdAsync(conn, categoryId);

                return new AIToolDto
                {
                    ToolId = toolId,
                    Name = name,
                    Description = description,
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    WebsiteURL = websiteUrl,
                    CreatedAt = createdAt
                };
            }
        }

        public async Task<AIToolDto> EditAsync(int toolId, AIToolCreateDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"UPDATE AITools
          SET Name = @Name, Description = @Description, CategoryId = @CategoryId, WebsiteURL = @WebsiteURL
          OUTPUT INSERTED.ToolId, INSERTED.Name, INSERTED.Description, INSERTED.CategoryId, INSERTED.WebsiteURL, INSERTED.CreatedAt
          WHERE ToolId = @ToolId", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", toolId);
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoryId", dto.CategoryId);
                cmd.Parameters.AddWithValue("@WebsiteURL", (object)dto.WebsiteURL ?? DBNull.Value);

                await conn.OpenAsync();

                int categoryId = 0;
                string name = "", description = null, websiteUrl = null;
                DateTime createdAt = DateTime.UtcNow;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        toolId = reader.GetInt32(0);
                        name = reader.GetString(1);
                        description = reader.IsDBNull(2) ? null : reader.GetString(2);
                        categoryId = reader.GetInt32(3);
                        websiteUrl = reader.IsDBNull(4) ? null : reader.GetString(4);
                        createdAt = reader.GetDateTime(5);
                    }
                }

                // Safe second query
                var categoryName = await GetCategoryNameByIdAsync(conn, categoryId);

                return new AIToolDto
                {
                    ToolId = toolId,
                    Name = name,
                    Description = description,
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    WebsiteURL = websiteUrl,
                    CreatedAt = createdAt
                };
            }
        }

        public async Task<bool> DeleteAsync(int toolId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("DELETE FROM AITools WHERE ToolId = @ToolId", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", toolId);
                await conn.OpenAsync();
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        // Helper to get CategoryName by ID (keep connection open)
        private async Task<string> GetCategoryNameByIdAsync(SqlConnection conn, int categoryId)
        {
            using (var cmd = new SqlCommand("SELECT CategoryName FROM Categories WHERE CategoryId = @CategoryId", conn))
            {
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                var result = await cmd.ExecuteScalarAsync();
                return result as string;
            }
        }
    }
}
