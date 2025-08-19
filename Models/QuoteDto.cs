using System.ComponentModel.DataAnnotations;

namespace itarixapi.Models
{
    public class QuoteDto
    {
        [Required]
        public string Service { get; set; } = string.Empty;

        [Required]
        public string Tier { get; set; } = string.Empty;

        public string? Type { get; set; } // from JS: payload.type

        [Range(1, 999)]
        public int Pages { get; set; } = 1;

        public List<string> Features { get; set; } = new();

        public List<string> Platforms { get; set; } = new();

        [Range(0, int.MaxValue)]
        public int PriceNumber { get; set; } // raw number in €

        [Range(0, int.MaxValue)]
        public int EstimatedHours { get; set; }

        public bool SaveToAccount { get; set; } = true;

        public string? Note { get; set; }

    }

    public class QuoteRecord : QuoteDto
    {
        public string QuoteId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
