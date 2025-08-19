namespace itarixapi.Models
{
    public class AIToolDto
    {
        public int ToolId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }            // Use int instead of string Category
        public string CategoryName { get; set; }       // Optional: add if you want to show the name
        public string WebsiteURL { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // For creating a tool (admin only, maybe)
    public class AIToolCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }           // Use int instead of string Category
        public string WebsiteURL { get; set; }
    }

}
