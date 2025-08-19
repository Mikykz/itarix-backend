namespace itarixapi.Models
{
    // For listing or showing comment details
    public class ToolCommentDto
    {
        public int CommentId { get; set; }
        public int? ReviewId { get; set; }    // changed to nullable
        public int ToolId { get; set; }       // add this!
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? ParentCommentId { get; set; }
        public string CommentText { get; set; }
        public bool IsApproved { get; set; }
        public bool IsFlagged { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ToolCommentCreateDto
    {
        public int? ReviewId { get; set; }    // changed to nullable
        public int ToolId { get; set; }       // add this!
        public int? ParentCommentId { get; set; }
        public string CommentText { get; set; }
    }


}
