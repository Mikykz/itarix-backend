using Itarix.Api.Models;
using itarixapi.Data;
using itarixapi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace itarixapi.Business
{
    public interface IModerationService
    {
        Task<List<ToolReviewDto>> GetPendingReviewsAsync();
        Task<ServiceResult<bool>> ApproveReviewAsync(int reviewId);
        Task<ServiceResult<bool>> DeleteReviewAsync(int reviewId);
        // Extend for comments if needed
    }

    public class ModerationService : IModerationService
    {
        private readonly IModerationRepository _repository;
        public ModerationService(IModerationRepository repository) { _repository = repository; }

        public async Task<List<ToolReviewDto>> GetPendingReviewsAsync()
        {
            return await _repository.GetPendingReviewsAsync();
        }

        public async Task<ServiceResult<bool>> ApproveReviewAsync(int reviewId)
        {
            try
            {
                var result = await _repository.ApproveReviewAsync(reviewId);
                return !result ? ServiceResult<bool>.Fail("Approve failed.") : ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var result = await _repository.DeleteReviewAsync(reviewId);
                return !result ? ServiceResult<bool>.Fail("Delete failed.") : ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }
    }
}
