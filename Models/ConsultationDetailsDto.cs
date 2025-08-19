namespace Itarix.Api.Models
{
    public class ConsultationDetailsDto
    {
        public int ConsultationId { get; set; }
        public int ServiceTypeId { get; set; }
        public string ServiceTypeKey { get; set; }    // optional
        public string ServiceTypeTitle { get; set; }  // optional
        public DateTime CreatedAt { get; set; }
        public List<ConsultationAnswerDto> Answers { get; set; }
    }
}
