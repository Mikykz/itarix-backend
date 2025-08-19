using Itarix.Api.Models;
using itarixapi.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace itarixapi.Data
{
    public class ToolReviewRepository : IToolReviewRepository
    {
        private readonly string _connectionString;

        public ToolReviewRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<ToolReviewDto>> GetReviewsByToolAsync(int toolId)
        {
            var reviews = new List<ToolReviewDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"SELECT r.ReviewId, r.ToolId, r.UserId, u.UserName, r.Rating, r.ReviewText, r.IsApproved, r.IsFlagged, r.CreatedAt, r.UpdatedAt
          FROM ToolReviews r
          JOIN Users u ON r.UserId = u.UserId
          WHERE r.ToolId = @ToolId", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", toolId);
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        reviews.Add(new ToolReviewDto
                        {
                            ReviewId = reader.GetInt32(0),
                            ToolId = reader.GetInt32(1),
                            UserId = reader.GetInt32(2),
                            UserName = reader.IsDBNull(3) ? null : reader.GetString(3), // NEW!
                            Rating = reader.GetInt32(4),
                            ReviewText = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsApproved = reader.GetBoolean(6),
                            IsFlagged = reader.GetBoolean(7),
                            CreatedAt = reader.GetDateTime(8),
                            UpdatedAt = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9)
                        });
                    }
                }
            }
            return reviews;
        }


        public async Task<ToolReviewDto> CreateReviewAsync(ToolReviewCreateDto dto, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"INSERT INTO ToolReviews (ToolId, UserId, Rating, ReviewText, IsApproved, IsFlagged, CreatedAt) 
                  OUTPUT INSERTED.ReviewId, INSERTED.ToolId, INSERTED.UserId, INSERTED.Rating, INSERTED.ReviewText, 
                         INSERTED.IsApproved, INSERTED.IsFlagged, INSERTED.CreatedAt, INSERTED.UpdatedAt
                  VALUES (@ToolId, @UserId, @Rating, @ReviewText, 0, 0, GETDATE())", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", dto.ToolId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Rating", dto.Rating);
                cmd.Parameters.AddWithValue("@ReviewText", (object)dto.ReviewText ?? DBNull.Value);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ToolReviewDto
                        {
                            ReviewId = reader.GetInt32(0),
                            ToolId = reader.GetInt32(1),
                            UserId = reader.GetInt32(2),
                            Rating = reader.GetInt32(3),
                            ReviewText = reader.IsDBNull(4) ? null : reader.GetString(4),
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

        public async Task<ToolReviewDto> EditReviewAsync(int reviewId, ToolReviewCreateDto dto, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"UPDATE ToolReviews 
                  SET Rating = @Rating, ReviewText = @ReviewText, UpdatedAt = GETDATE()
                  OUTPUT INSERTED.ReviewId, INSERTED.ToolId, INSERTED.UserId, INSERTED.Rating, 
                         INSERTED.ReviewText, INSERTED.IsApproved, INSERTED.IsFlagged, 
                         INSERTED.CreatedAt, INSERTED.UpdatedAt
                  WHERE ReviewId = @ReviewId AND UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", reviewId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Rating", dto.Rating);
                cmd.Parameters.AddWithValue("@ReviewText", (object)dto.ReviewText ?? DBNull.Value);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ToolReviewDto
                        {
                            ReviewId = reader.GetInt32(0),
                            ToolId = reader.GetInt32(1),
                            UserId = reader.GetInt32(2),
                            Rating = reader.GetInt32(3),
                            ReviewText = reader.IsDBNull(4) ? null : reader.GetString(4),
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

        public async Task<bool> DeleteReviewAsync(int reviewId, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("DELETE FROM ToolReviews WHERE ReviewId = @ReviewId AND UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", reviewId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await conn.OpenAsync();
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        public async Task<bool> ReportReviewAsync(ReviewReportCreateDto dto, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                @"INSERT INTO ReviewReports (ReviewId, UserId, Reason, CreatedAt) 
                  VALUES (@ReviewId, @UserId, @Reason, GETDATE())", conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", dto.ReviewId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Reason", dto.Reason);

                await conn.OpenAsync();
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        // NEW: Average rating and count for a tool
        public async Task<(double average, int count)> GetAverageRatingAsync(int toolId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*), AVG(CAST(Rating AS FLOAT)) FROM ToolReviews WHERE ToolId=@ToolId", conn))
            {
                cmd.Parameters.AddWithValue("@ToolId", toolId);
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int count = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        double avg = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        return (avg, count);
                    }
                }
            }
            return (0, 0);
        }
    }
}
