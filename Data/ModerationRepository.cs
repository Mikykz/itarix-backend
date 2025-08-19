using Itarix.Api.Models;
using itarixapi.Models;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace itarixapi.Data
{
    public interface IModerationRepository
    {
        Task<List<ToolReviewDto>> GetPendingReviewsAsync();
        Task<bool> ApproveReviewAsync(int reviewId);
        Task<bool> DeleteReviewAsync(int reviewId);
        // Extend for comments if needed
    }

    public class ModerationRepository : IModerationRepository
    {
        private readonly string _connectionString;

        public ModerationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<List<ToolReviewDto>> GetPendingReviewsAsync()
        {
            var reviews = new List<ToolReviewDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "SELECT ReviewId, ToolId, UserId, Rating, ReviewText, IsApproved, IsFlagged, CreatedAt, UpdatedAt " +
                "FROM ToolReviews WHERE IsApproved = 0", conn))
            {
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
                            Rating = reader.GetInt32(3),
                            ReviewText = reader.IsDBNull(4) ? null : reader.GetString(4),
                            IsApproved = reader.GetBoolean(5),
                            IsFlagged = reader.GetBoolean(6),
                            CreatedAt = reader.GetDateTime(7),
                            UpdatedAt = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8)
                        });
                    }
                }
            }
            return reviews;
        }

        public async Task<bool> ApproveReviewAsync(int reviewId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "UPDATE ToolReviews SET IsApproved = 1 WHERE ReviewId = @ReviewId", conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", reviewId);
                await conn.OpenAsync();
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "DELETE FROM ToolReviews WHERE ReviewId = @ReviewId", conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", reviewId);
                await conn.OpenAsync();
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }
    }
}
