using System;

namespace Domain.Entities
{    public class PreliminaryProject : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        public virtual StateStage? StateStage { get; set; }
    }
}