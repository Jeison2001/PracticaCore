using System;
using Application.Shared.DTOs.StatePreliminaryProject;

namespace Application.Shared.DTOs.PreliminaryProject
{
    public class PreliminaryProjectDto : BaseDto<int>
    {
        public int IdStatePreliminaryProject { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public StatePreliminaryProjectDto? StatePreliminaryProject { get; set; }
    }
}