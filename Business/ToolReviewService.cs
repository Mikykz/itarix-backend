using Itarix.Api.Models;
using itarixapi.Data;
using itarixapi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace itarixapi.Business
{
    public interface IToolReviewService
    {
        Task<List<ToolReviewDto>> GetReviewsByToolAsync(int toolId);
        Task<ServiceResult<ToolReviewDto>> CreateReviewAsync(ToolReviewCreateDto dto, int userId);
        Task<ServiceResult<ToolReviewDto>> EditReviewAsync(int reviewId, ToolReviewCreateDto dto, int userId);
        Task<ServiceResult<bool>> DeleteReviewAsync(int reviewId, int userId);
        Task<ServiceResult<bool>> ReportReviewAsync(ReviewReportCreateDto dto, int userId);

        // NEW: For average rating and count
        Task<(double average, int count)> GetAverageRatingAsync(int toolId);
    }

    public class ToolReviewService : IToolReviewService
    {
        private readonly IToolReviewRepository _repository;
        public ToolReviewService(IToolReviewRepository repository) { _repository = repository; }

        public async Task<List<ToolReviewDto>> GetReviewsByToolAsync(int toolId)
        {
            return await _repository.GetReviewsByToolAsync(toolId);
        }

        public async Task<ServiceResult<ToolReviewDto>> CreateReviewAsync(ToolReviewCreateDto dto, int userId)
        {
            try
            {
                var review = await _repository.CreateReviewAsync(dto, userId);
                return review == null ? ServiceResult<ToolReviewDto>.Fail("Could not create review.") : ServiceResult<ToolReviewDto>.Ok(review);
            }
            catch (Exception ex)
            {
                return ServiceResult<ToolReviewDto>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<ToolReviewDto>> EditReviewAsync(int reviewId, ToolReviewCreateDto dto, int userId)
        {
            try
            {
                var review = await _repository.EditReviewAsync(reviewId, dto, userId);
                return review == null ? ServiceResult<ToolReviewDto>.Fail("Review not found or update failed.") : ServiceResult<ToolReviewDto>.Ok(review);
            }
            catch (Exception ex)
            {
                return ServiceResult<ToolReviewDto>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> DeleteReviewAsync(int reviewId, int userId)
        {
            try
            {
                var result = await _repository.DeleteReviewAsync(reviewId, userId);
                return !result ? ServiceResult<bool>.Fail("Delete failed.") : ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> ReportReviewAsync(ReviewReportCreateDto dto, int userId)
        {
            try
            {
                var result = await _repository.ReportReviewAsync(dto, userId);
                return !result ? ServiceResult<bool>.Fail("Report failed.") : ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        // NEW: For average rating/count
        public async Task<(double average, int count)> GetAverageRatingAsync(int toolId)
        {
            return await _repository.GetAverageRatingAsync(toolId);
        }
    }
}
