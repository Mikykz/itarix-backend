using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Itarix.Api.Data;
using Itarix.Api.Models;

namespace Itarix.Api.Business
{
    public class ITConsultationService
    {
        private readonly ITConsultationRepository _repository;

        public ITConsultationService(ITConsultationRepository repository)
        {
            _repository = repository;
        }

        // Create
        public async Task<int> AddConsultationWithAnswers(ConsultationDto dto, int userId)
        {
            var consultation = new ITConsultation
            {
                UserId = userId,
                ServiceTypeId = dto.ServiceTypeId,
                Status = string.IsNullOrEmpty(dto.Status) ? "draft" : dto.Status,
                CreatedAt = DateTime.UtcNow
            };

            var answers = new List<ITConsultationAnswer>();
            if (dto.Answers != null)
            {
                foreach (var ans in dto.Answers)
                {
                    answers.Add(new ITConsultationAnswer
                    {
                        QuestionId = ans.QuestionId,
                        AnswerValue = ans.AnswerValue,
                        AnsweredAt = DateTime.UtcNow,
                        SectionKey = ans.SectionKey,
                        QuestionKey = ans.QuestionKey
                    });
                }
            }

            return await _repository.AddConsultationWithAnswersAsync(consultation, answers);
        }

        // ----- USER-SCOPED LISTING -----

        public Task<List<ITConsultation>> GetConsultationsByUser(int userId)
            => _repository.GetConsultationsByUserAsync(userId);

        public Task<List<ITConsultation>> GetConsultationsByUserPaged(
            int userId, int limit, int offset, string status = null, int? serviceTypeId = null,
            DateTime? from = null, DateTime? to = null)
            => _repository.GetConsultationsByUserPagedAsync(userId, limit, offset, status, serviceTypeId, from, to);

        public Task<int> GetConsultationsByUserCount(
            int userId, string status = null, int? serviceTypeId = null,
            DateTime? from = null, DateTime? to = null)
            => _repository.GetConsultationsByUserCountAsync(userId, status, serviceTypeId, from, to);

        public Task<ITConsultation> GetConsultationDetailsByIdForUser(int consultationId, int userId)
            => _repository.GetConsultationByIdForUserAsync(consultationId, userId);

        public Task<ITConsultation> GetLatestConsultationForUser(int userId)
            => _repository.GetLatestConsultationForUserAsync(userId);

        // ----- ADMIN / BACKOFFICE -----

        public Task<ITConsultation> GetConsultationDetailsById(int id)
            => _repository.GetConsultationByIdAsync(id);
    }
}
