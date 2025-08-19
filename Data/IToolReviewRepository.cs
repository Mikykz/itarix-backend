using System.Collections.Generic;
using System.Threading.Tasks;
using Itarix.Api.Models;
using itarixapi.Models;

namespace itarixapi.Data
{
    public interface IToolReviewRepository
    {
        Task<List<ToolReviewDto>> GetReviewsByToolAsync(int toolId);
        Task<ToolReviewDto> CreateReviewAsync(ToolReviewCreateDto dto, int userId);
        Task<ToolReviewDto> EditReviewAsync(int reviewId, ToolReviewCreateDto dto, int userId);
        Task<bool> DeleteReviewAsync(int reviewId, int userId);
        Task<bool> ReportReviewAsync(ReviewReportCreateDto dto, int userId);
        Task<(double average, int count)> GetAverageRatingAsync(int toolId);
    }
}
