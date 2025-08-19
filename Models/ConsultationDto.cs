namespace Itarix.Api.Models
{
    public class ConsultationDto
    {
        public int ConsultationId { get; set; }
        public int UserId { get; set; }
        public string ServiceType { get; set; }  // optional
        public int ServiceTypeId { get; set; }
        public string Status { get; set; }
        public List<ConsultationAnswerDto> Answers { get; set; }
    }

    public class ConsultationAnswerDto
    {
        public int ConsultationId { get; set; }
        public int QuestionId { get; set; }
        public string AnswerValue { get; set; }
        public string SectionKey { get; set; }
        public string QuestionKey { get; set; }
    }
}
