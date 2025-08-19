using Itarix.Api.Models;
using itarixapi.Models;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace itarixapi.Data
{
    public interface IToolCommentRepository
    {
        Task<List<ToolCommentDto>> GetCommentsByReviewAsync(int reviewId);
        Task<ToolCommentDto> CreateCommentAsync(ToolCommentCreateDto dto, int userId);
        Task<ToolCommentDto> EditCommentAsync(int commentId, ToolCommentCreateDto dto, int userId);
        Task<bool> DeleteCommentAsync(int commentId, int userId);
        Task<bool> ReportCommentAsync(CommentReportCreateDto dto, int userId);

        Task<List<ToolCommentDto>> GetCommentsByToolAsync(int toolId);
        Task<int> GetCommentsCountByToolAsync(int toolId);

    }

    public class ToolCommentRepository : IToolCommentRepository
    {
        private readonly string _connectionString;

        public async Task<List<ToolCommentDto>> GetCommentsByToolAsync(int toolId)
        {
            var comments = new List<ToolCommentDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"SELECT 
            c.CommentId, 
            c.ReviewId, 
            c.ToolId, 
            c.UserId, 
            u.UserName, 
            c.ParentCommentId, 
            c.CommentText, 
            c.IsApproved, 
            c.IsFlagged, 
            c.CreatedAt, 
            c.UpdatedAt
        FROM ToolComments c
        JOIN Users u ON c.UserId = u.UserId
        WHERE c.ToolId = @ToolId", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", toolId);
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        comments.Add(new ToolCommentDto
                        {
                            CommentId = reader.GetInt32(0),
                            ReviewId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            ToolId = reader.GetInt32(2),
                            UserId = reader.GetInt32(3),
                            UserName = reader.IsDBNull(4) ? null : reader.GetString(4),
                            ParentCommentId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            CommentText = reader.IsDBNull(6) ? null : reader.GetString(6),
                            IsApproved = reader.GetBoolean(7),
                            IsFlagged = reader.GetBoolean(8),
                            CreatedAt = reader.GetDateTime(9),
                            UpdatedAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10)
                        });
                    }
                }
            }
            return comments;
        }



        public async Task<int> GetCommentsCountByToolAsync(int toolId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM ToolComments WHERE ToolId=@ToolId", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", toolId);
                await conn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync();
            }
        }


        public ToolCommentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<ToolCommentDto>> GetCommentsByReviewAsync(int reviewId)
        {
            var comments = new List<ToolCommentDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "SELECT CommentId, ReviewId, UserId, ParentCommentId, CommentText, IsApproved, IsFlagged, CreatedAt, UpdatedAt FROM ToolComments WHERE ReviewId=@ReviewId", conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", reviewId);
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        comments.Add(new ToolCommentDto
                        {
                            CommentId = reader.GetInt32(0),
                            ReviewId = reader.GetInt32(1),
                            UserId = reader.GetInt32(2),
                            ParentCommentId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            CommentText = reader.IsDBNull(4) ? null : reader.GetString(4),
                            IsApproved = reader.GetBoolean(5),
                            IsFlagged = reader.GetBoolean(6),
                            CreatedAt = reader.GetDateTime(7),
                            UpdatedAt = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8)
                        });
                    }
                }
            }
            return comments;
        }

        public async Task<ToolCommentDto> CreateCommentAsync(ToolCommentCreateDto dto, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"INSERT INTO ToolComments (ReviewId, ToolId, UserId, ParentCommentId, CommentText, IsApproved, IsFlagged, CreatedAt)
          OUTPUT INSERTED.CommentId, INSERTED.ReviewId, INSERTED.ToolId, INSERTED.UserId, INSERTED.ParentCommentId,
                 INSERTED.CommentText, INSERTED.IsApproved, INSERTED.IsFlagged, INSERTED.CreatedAt, INSERTED.UpdatedAt
          VALUES (@ReviewId, @ToolId, @UserId, @ParentCommentId, @CommentText, 0, 0, GETDATE())", conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", (object)dto.ReviewId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToolId", dto.ToolId); // required!
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ParentCommentId", (object)dto.ParentCommentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CommentText", (object)dto.CommentText ?? DBNull.Value);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ToolCommentDto
                        {
                            CommentId = reader.GetInt32(0),
                            ReviewId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            ToolId = reader.GetInt32(2),
                            UserId = reader.GetInt32(3),
                            ParentCommentId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            CommentText = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsApproved = reader.GetBoolean(6),
                            IsFlagged = reader.GetBoolean(7),
                            CreatedAt = reader.GetDateTime(8),
                            UpdatedAt = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9)
                        };
                    }
                }
            }
            return null;
        }


        public async Task<ToolCommentDto> EditCommentAsync(int commentId, ToolCommentCreateDto dto, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"UPDATE ToolComments
                  SET CommentText = @CommentText, UpdatedAt = GETDATE()
                  OUTPUT INSERTED.CommentId, INSERTED.ReviewId, INSERTED.UserId, INSERTED.ParentCommentId,
                         INSERTED.CommentText, INSERTED.IsApproved, INSERTED.IsFlagged, INSERTED.CreatedAt, INSERTED.UpdatedAt
                  WHERE CommentId = @CommentId AND UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@CommentId", commentId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CommentText", (object)dto.CommentText ?? DBNull.Value);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ToolCommentDto
                        {
                            CommentId = reader.GetInt32(0),
                            ReviewId = reader.GetInt32(1),
                            UserId = reader.GetInt32(2),
                            ParentCommentId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            CommentText = reader.IsDBNull(4) ? null : reader.GetString(4),
                            IsApproved = reader.GetBoolean(5),
                            IsFlagged = reader.GetBoolean(6),
                            CreatedAt = reader.GetDateTime(7),
                            UpdatedAt = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8)
                        };
                    }
                }
            }
            return null;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("DELETE FROM ToolComments WHERE CommentId = @CommentId AND UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@CommentId", commentId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await conn.OpenAsync();
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        public async Task<bool> ReportCommentAsync(CommentReportCreateDto dto, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"INSERT INTO CommentReports (CommentId, UserId, Reason, CreatedAt)
                  VALUES (@CommentId, @UserId, @Reason, GETDATE())", conn))
            {
                cmd.Parameters.AddWithValue("@CommentId", dto.CommentId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Reason", dto.Reason);

                await conn.OpenAsync();
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }
    }
}
