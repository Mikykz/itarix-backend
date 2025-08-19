using Itarix.Api.Models;
using itarixapi.Data;
using itarixapi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace itarixapi.Business
{
    public interface IToolCommentService
    {
        Task<List<ToolCommentDto>> GetCommentsByToolAsync(int toolId);
        Task<int> GetCommentsCountByToolAsync(int toolId);

        Task<List<ToolCommentDto>> GetCommentsByReviewAsync(int reviewId);
        Task<ServiceResult<ToolCommentDto>> CreateCommentAsync(ToolCommentCreateDto dto, int userId);
        Task<ServiceResult<ToolCommentDto>> EditCommentAsync(int commentId, ToolCommentCreateDto dto, int userId);
        Task<ServiceResult<bool>> DeleteCommentAsync(int commentId, int userId);
        Task<ServiceResult<bool>> ReportCommentAsync(CommentReportCreateDto dto, int userId);
    }

    public class ToolCommentService : IToolCommentService
    {   
        private readonly IToolCommentRepository _repository;

        public async Task<List<ToolCommentDto>> GetCommentsByToolAsync(int toolId)
        {
            return await _repository.GetCommentsByToolAsync(toolId);
        }

        public async Task<int> GetCommentsCountByToolAsync(int toolId)
        {
            return await _repository.GetCommentsCountByToolAsync(toolId);
        }

        public ToolCommentService(IToolCommentRepository repository) { _repository = repository; }

        public async Task<List<ToolCommentDto>> GetCommentsByReviewAsync(int reviewId)
        {
            return await _repository.GetCommentsByReviewAsync(reviewId);
        }

        public async Task<ServiceResult<ToolCommentDto>> CreateCommentAsync(ToolCommentCreateDto dto, int userId)
        {
            try
            {
                var comment = await _repository.CreateCommentAsync(dto, userId);
                return comment == null ? ServiceResult<ToolCommentDto>.Fail("Could not create comment.") : ServiceResult<ToolCommentDto>.Ok(comment);
            }
            catch (Exception ex)
            {
                return ServiceResult<ToolCommentDto>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<ToolCommentDto>> EditCommentAsync(int commentId, ToolCommentCreateDto dto, int userId)
        {
            try
            {
                var comment = await _repository.EditCommentAsync(commentId, dto, userId);
                return comment == null ? ServiceResult<ToolCommentDto>.Fail("Comment not found or update failed.") : ServiceResult<ToolCommentDto>.Ok(comment);
            }
            catch (Exception ex)
            {
                return ServiceResult<ToolCommentDto>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> DeleteCommentAsync(int commentId, int userId)
        {
            try
            {
                var result = await _repository.DeleteCommentAsync(commentId, userId);
                return !result ? ServiceResult<bool>.Fail("Delete failed.") : ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> ReportCommentAsync(CommentReportCreateDto dto, int userId)
        {
            try
            {
                var result = await _repository.ReportCommentAsync(dto, userId);
                return !result ? ServiceResult<bool>.Fail("Report failed.") : ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }
    }
}
