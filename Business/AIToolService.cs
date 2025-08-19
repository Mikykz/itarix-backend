using Itarix.Api.Models;
using itarixapi.Data;
using itarixapi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace itarixapi.Business
{
    public interface IAIToolService
    {
        Task<List<AIToolDto>> GetAllAsync();
        Task<AIToolDto> GetByIdAsync(int toolId);
        Task<ServiceResult<AIToolDto>> CreateAsync(AIToolCreateDto dto);
        Task<ServiceResult<AIToolDto>> EditAsync(int toolId, AIToolCreateDto dto);
        Task<ServiceResult<bool>> DeleteAsync(int toolId);
    }

    public class AIToolService : IAIToolService
    {
        private readonly IAIToolRepository _repository;
        public AIToolService(IAIToolRepository repository) { _repository = repository; }

        public async Task<List<AIToolDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<AIToolDto> GetByIdAsync(int toolId)
        {
            return await _repository.GetByIdAsync(toolId);
        }

        public async Task<ServiceResult<AIToolDto>> CreateAsync(AIToolCreateDto dto)
        {
            try
            {
                var tool = await _repository.CreateAsync(dto);
                return tool == null ? ServiceResult<AIToolDto>.Fail("Create failed.") : ServiceResult<AIToolDto>.Ok(tool);
            }
            catch (Exception ex)
            {
                return ServiceResult<AIToolDto>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<AIToolDto>> EditAsync(int toolId, AIToolCreateDto dto)
        {
            try
            {
                var tool = await _repository.EditAsync(toolId, dto);
                return tool == null ? ServiceResult<AIToolDto>.Fail("Edit failed or tool not found.") : ServiceResult<AIToolDto>.Ok(tool);
            }
            catch (Exception ex)
            {
                return ServiceResult<AIToolDto>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int toolId)
        {
            try
            {
                var result = await _repository.DeleteAsync(toolId);
                return !result ? ServiceResult<bool>.Fail("Delete failed.") : ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }
    }
}
