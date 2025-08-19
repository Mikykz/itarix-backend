namespace Itarix.Api.Models
{
    public class ITConsultation
    {
        public int ConsultationId { get; set; }
        public int UserId { get; set; }
        public int ServiceTypeId { get; set; }
        public string ServiceType { get; set; }   // optional
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public List<ITConsultationAnswer> Answers { get; set; }
    }
}
