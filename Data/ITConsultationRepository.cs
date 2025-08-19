using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Itarix.Api.Models;
using Microsoft.Extensions.Configuration;

namespace Itarix.Api.Data
{
    public class ITConsultationRepository
    {
        private readonly string _connectionString;

        public ITConsultationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // 1) Add a new consultation + answers (transactional, supports multi-select)
        public async Task<int> AddConsultationWithAnswersAsync(ITConsultation consultation, List<ITConsultationAnswer> answers)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var transaction = conn.BeginTransaction();
            try
            {
                int consultationId;

                // Insert header
                await using (var cmd = new SqlCommand(@"
                    INSERT INTO ITConsultations (UserId, ServiceTypeId, CreatedAt, Status)
                    VALUES (@UserId, @ServiceTypeId, @CreatedAt, @Status);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId", consultation.UserId);
                    cmd.Parameters.AddWithValue("@ServiceTypeId", consultation.ServiceTypeId);
                    cmd.Parameters.AddWithValue("@CreatedAt", consultation.CreatedAt);
                    cmd.Parameters.AddWithValue("@Status", (object)consultation.Status ?? DBNull.Value);

                    consultationId = (int)await cmd.ExecuteScalarAsync();
                }

                // Insert answers
                if (answers != null && answers.Count > 0)
                {
                    foreach (var answer in answers)
                    {
                        int answerId;
                        await using (var cmd = new SqlCommand(@"
                            INSERT INTO ITConsultationAnswers 
                            (ConsultationId, QuestionId, AnswerValue, AnsweredAt, SectionKey, QuestionKey)
                            OUTPUT INSERTED.AnswerId
                            VALUES (@ConsultationId, @QuestionId, @AnswerValue, @AnsweredAt, @SectionKey, @QuestionKey);", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ConsultationId", consultationId);
                            cmd.Parameters.AddWithValue("@QuestionId", answer.QuestionId);
                            cmd.Parameters.AddWithValue("@AnswerValue", string.IsNullOrEmpty(answer.AnswerValue) ? (object)DBNull.Value : answer.AnswerValue);
                            cmd.Parameters.AddWithValue("@AnsweredAt", answer.AnsweredAt);
                            cmd.Parameters.AddWithValue("@SectionKey", string.IsNullOrEmpty(answer.SectionKey) ? (object)DBNull.Value : answer.SectionKey);
                            cmd.Parameters.AddWithValue("@QuestionKey", string.IsNullOrEmpty(answer.QuestionKey) ? (object)DBNull.Value : answer.QuestionKey);

                            answerId = (int)await cmd.ExecuteScalarAsync();
                        }

                        // Insert multi-select rows (if any)
                        if (answer.MultiSelectOptionIds != null && answer.MultiSelectOptionIds.Count > 0)
                        {
                            foreach (var optionId in answer.MultiSelectOptionIds)
                            {
                                await using var msCmd = new SqlCommand(@"
                                    INSERT INTO ITConsultationMultiSelectAnswers 
                                    (AnswerId, OptionId, AnsweredAt)
                                    VALUES (@AnswerId, @OptionId, @AnsweredAt);", conn, transaction);
                                msCmd.Parameters.AddWithValue("@AnswerId", answerId);
                                msCmd.Parameters.AddWithValue("@OptionId", optionId);
                                msCmd.Parameters.AddWithValue("@AnsweredAt", answer.AnsweredAt);
                                await msCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                await transaction.CommitAsync();
                return consultationId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 2) Get all consultations for a user (ordered)
        public async Task<List<ITConsultation>> GetConsultationsByUserAsync(int userId)
        {
            var consultations = new List<ITConsultation>();

            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(@"
                SELECT ConsultationId, UserId, ServiceTypeId, CreatedAt, Status
                FROM ITConsultations
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC;", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                consultations.Add(new ITConsultation
                {
                    ConsultationId = (int)reader["ConsultationId"],
                    UserId = (int)reader["UserId"],
                    ServiceTypeId = (int)reader["ServiceTypeId"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    Status = reader["Status"] as string
                });
            }

            return consultations;
        }

        // 2a) Paged + filterable list for a user
        public async Task<List<ITConsultation>> GetConsultationsByUserPagedAsync(
            int userId,
            int limit,
            int offset,
            string status = null,
            int? serviceTypeId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var results = new List<ITConsultation>();

            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(@"
                SELECT ConsultationId, UserId, ServiceTypeId, CreatedAt, Status
                FROM ITConsultations
                WHERE UserId = @UserId
                  AND (@Status IS NULL OR Status = @Status)
                  AND (@ServiceTypeId IS NULL OR ServiceTypeId = @ServiceTypeId)
                  AND (@From IS NULL OR CreatedAt >= @From)
                  AND (@To   IS NULL OR CreatedAt <  @To)
                ORDER BY CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ServiceTypeId", (object)serviceTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@From", (object)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@To", (object)to ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Offset", offset < 0 ? 0 : offset);
            cmd.Parameters.AddWithValue("@Limit", limit <= 0 ? 25 : limit);

            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ITConsultation
                {
                    ConsultationId = (int)reader["ConsultationId"],
                    UserId = (int)reader["UserId"],
                    ServiceTypeId = (int)reader["ServiceTypeId"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    Status = reader["Status"] as string
                });
            }

            return results;
        }

        // 2b) Count for pagination (same filters)
        public async Task<int> GetConsultationsByUserCountAsync(
            int userId,
            string status = null,
            int? serviceTypeId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM ITConsultations
                WHERE UserId = @UserId
                  AND (@Status IS NULL OR Status = @Status)
                  AND (@ServiceTypeId IS NULL OR ServiceTypeId = @ServiceTypeId)
                  AND (@From IS NULL OR CreatedAt >= @From)
                  AND (@To   IS NULL OR CreatedAt <  @To);", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ServiceTypeId", (object)serviceTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@From", (object)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@To", (object)to ?? DBNull.Value);

            await conn.OpenAsync();
            var scalar = await cmd.ExecuteScalarAsync();
            return scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
        }

        // 3) Secure single consultation by id for the OWNER
        public async Task<ITConsultation> GetConsultationByIdForUserAsync(int consultationId, int userId)
        {
            ITConsultation consultation = null;

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // header
            await using (var cmd = new SqlCommand(@"
                SELECT ConsultationId, UserId, ServiceTypeId, CreatedAt, Status
                FROM ITConsultations
                WHERE ConsultationId = @ConsultationId AND UserId = @UserId;", conn))
            {
                cmd.Parameters.AddWithValue("@ConsultationId", consultationId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    consultation = new ITConsultation
                    {
                        ConsultationId = (int)reader["ConsultationId"],
                        UserId = (int)reader["UserId"],
                        ServiceTypeId = (int)reader["ServiceTypeId"],
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        Status = reader["Status"] as string,
                        Answers = new List<ITConsultationAnswer>()
                    };
                }
            }

            if (consultation == null) return null;

            // answers
            var answers = new List<ITConsultationAnswer>();
            await using (var answerCmd = new SqlCommand(@"
                SELECT AnswerId, ConsultationId, QuestionId, AnswerValue, AnsweredAt, SectionKey, QuestionKey
                FROM ITConsultationAnswers
                WHERE ConsultationId = @ConsultationId;", conn))
            {
                answerCmd.Parameters.AddWithValue("@ConsultationId", consultationId);
                await using var reader = await answerCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    answers.Add(new ITConsultationAnswer
                    {
                        AnswerId = (int)reader["AnswerId"],
                        ConsultationId = (int)reader["ConsultationId"],
                        QuestionId = (int)reader["QuestionId"],
                        AnswerValue = reader["AnswerValue"] as string ?? string.Empty,
                        AnsweredAt = (DateTime)reader["AnsweredAt"],
                        SectionKey = reader["SectionKey"] as string ?? string.Empty,
                        QuestionKey = reader["QuestionKey"] as string ?? string.Empty,
                        MultiSelectOptionIds = new List<int>()
                    });
                }
            }

            // multi-select
            if (answers.Count > 0)
            {
                var byId = new Dictionary<int, ITConsultationAnswer>(answers.Count);
                foreach (var a in answers) byId[a.AnswerId] = a;

                await using var msCmd = new SqlCommand(@"
                    SELECT msa.AnswerId, msa.OptionId
                    FROM ITConsultationMultiSelectAnswers msa
                    INNER JOIN ITConsultationAnswers a ON a.AnswerId = msa.AnswerId
                    WHERE a.ConsultationId = @ConsultationId;", conn);
                msCmd.Parameters.AddWithValue("@ConsultationId", consultationId);

                await using var msReader = await msCmd.ExecuteReaderAsync();
                while (await msReader.ReadAsync())
                {
                    int aid = (int)msReader["AnswerId"];
                    int opt = (int)msReader["OptionId"];
                    if (byId.TryGetValue(aid, out var ans))
                        ans.MultiSelectOptionIds.Add(opt);
                }
            }

            consultation.Answers = answers;
            return consultation;
        }

        // 3a) Admin detail by id
        public async Task<ITConsultation> GetConsultationByIdAsync(int consultationId)
        {
            ITConsultation consultation = null;

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // header
            await using (var cmd = new SqlCommand(@"
                SELECT ConsultationId, UserId, ServiceTypeId, CreatedAt, Status
                FROM ITConsultations
                WHERE ConsultationId = @ConsultationId;", conn))
            {
                cmd.Parameters.AddWithValue("@ConsultationId", consultationId);
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    consultation = new ITConsultation
                    {
                        ConsultationId = (int)reader["ConsultationId"],
                        UserId = (int)reader["UserId"],
                        ServiceTypeId = (int)reader["ServiceTypeId"],
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        Status = reader["Status"] as string,
                        Answers = new List<ITConsultationAnswer>()
                    };
                }
            }

            if (consultation == null) return null;

            // answers
            var answers = new List<ITConsultationAnswer>();
            await using (var answerCmd = new SqlCommand(@"
                SELECT AnswerId, ConsultationId, QuestionId, AnswerValue, AnsweredAt, SectionKey, QuestionKey
                FROM ITConsultationAnswers
                WHERE ConsultationId = @ConsultationId;", conn))
            {
                answerCmd.Parameters.AddWithValue("@ConsultationId", consultationId);
                await using var reader = await answerCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    answers.Add(new ITConsultationAnswer
                    {
                        AnswerId = (int)reader["AnswerId"],
                        ConsultationId = (int)reader["ConsultationId"],
                        QuestionId = (int)reader["QuestionId"],
                        AnswerValue = reader["AnswerValue"] as string ?? string.Empty,
                        AnsweredAt = (DateTime)reader["AnsweredAt"],
                        SectionKey = reader["SectionKey"] as string ?? string.Empty,
                        QuestionKey = reader["QuestionKey"] as string ?? string.Empty,
                        MultiSelectOptionIds = new List<int>()
                    });
                }
            }

            // multi-select
            if (answers.Count > 0)
            {
                var byId = new Dictionary<int, ITConsultationAnswer>(answers.Count);
                foreach (var a in answers) byId[a.AnswerId] = a;

                await using var msCmd = new SqlCommand(@"
                    SELECT msa.AnswerId, msa.OptionId
                    FROM ITConsultationMultiSelectAnswers msa
                    INNER JOIN ITConsultationAnswers a ON a.AnswerId = msa.AnswerId
                    WHERE a.ConsultationId = @ConsultationId;", conn);
                msCmd.Parameters.AddWithValue("@ConsultationId", consultationId);

                await using var msReader = await msCmd.ExecuteReaderAsync();
                while (await msReader.ReadAsync())
                {
                    int aid = (int)msReader["AnswerId"];
                    int opt = (int)msReader["OptionId"];
                    if (byId.TryGetValue(aid, out var ans))
                        ans.MultiSelectOptionIds.Add(opt);
                }
            }

            consultation.Answers = answers;
            return consultation;
        }

        // 4) Latest consultation for a user
        public async Task<ITConsultation> GetLatestConsultationForUserAsync(int userId)
        {
            ITConsultation consultation = null;

            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(@"
                SELECT TOP 1 ConsultationId, UserId, ServiceTypeId, CreatedAt, Status
                FROM ITConsultations
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC;", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                consultation = new ITConsultation
                {
                    ConsultationId = (int)reader["ConsultationId"],
                    UserId = (int)reader["UserId"],
                    ServiceTypeId = (int)reader["ServiceTypeId"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    Status = reader["Status"] as string
                };
            }

            return consultation;
        }
    }
}
