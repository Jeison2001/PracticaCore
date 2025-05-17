using System;

namespace Domain.Entities
{
    public class PreliminaryProject : BaseEntity<int>
    {
        public int IdStatePreliminaryProject { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        public StatePreliminaryProject? StatePreliminaryProject { get; set; }
    }
}