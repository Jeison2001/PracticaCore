using System;

namespace Application.Shared.DTOs.PreliminaryProject
{    public class PreliminaryProjectDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}