using System;
using Application.Shared.DTOs.StatePreliminaryProject;

namespace Application.Shared.DTOs.PreliminaryProject
{
    public class PreliminaryProjectDetailsDto : BaseDto<int>
    {
        public int IdStatePreliminaryProject { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public StatePreliminaryProjectDto? StatePreliminaryProject { get; set; }
        // Agrega aquí cualquier campo adicional que quieras exponer
    }
}
