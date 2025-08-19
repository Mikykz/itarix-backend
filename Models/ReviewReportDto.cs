namespace itarixapi.Models
{
    // For listing reports
    public class ReviewReportDto
    {
        public int ReportId { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // For creating a report
    public class ReviewReportCreateDto
    {
        public int ReviewId { get; set; }
        public string Reason { get; set; }
    }

}
