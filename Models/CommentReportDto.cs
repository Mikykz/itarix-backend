namespace itarixapi.Models
{
    // For listing reports
    public class CommentReportDto
    {
        public int ReportId { get; set; }
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // For creating a report
    public class CommentReportCreateDto
    {
        public int CommentId { get; set; }
        public string Reason { get; set; }
    }

}
