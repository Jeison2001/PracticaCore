using System;
using Application.Shared.DTOs.StateStage;

namespace Application.Shared.DTOs.PreliminaryProject
{
    public class PreliminaryProjectDetailsDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public StateStageDto? StateStage { get; set; }
        // Agrega aquí cualquier campo adicional que quieras exponer
    }
}
