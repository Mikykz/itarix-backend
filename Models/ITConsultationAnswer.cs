using System;
using System.Collections.Generic;

namespace Itarix.Api.Models
{
    public class ITConsultationAnswer
    {
        public int AnswerId { get; set; }
        public int ConsultationId { get; set; }
        public int QuestionId { get; set; }
        public string AnswerValue { get; set; }
        public string SectionKey { get; set; }
        public string QuestionKey { get; set; }
        public DateTime AnsweredAt { get; set; }

        // ADD THIS:
        public List<int> MultiSelectOptionIds { get; set; } = new List<int>();
    }
}
