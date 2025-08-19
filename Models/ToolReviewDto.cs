namespace itarixapi.Models
{
    // For listing or showing review details
    public class ToolReviewDto
    {
        public int ReviewId { get; set; }
        public int ToolId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }  // <-- Add this line!
        public int Rating { get; set; }
        public string ReviewText { get; set; }
        public bool IsApproved { get; set; }
        public bool IsFlagged { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // For creating a review
    public class ToolReviewCreateDto
    {
        public int ToolId { get; set; }
        public int Rating { get; set; }
        public string ReviewText { get; set; }
    }
}
