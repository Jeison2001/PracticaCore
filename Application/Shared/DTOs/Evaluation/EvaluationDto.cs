using Application.Shared.DTOs;

namespace Application.Shared.DTOs.Evaluation
{
    public class EvaluationDto : BaseDto<int>
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int IdEvaluationType { get; set; }
        public string? Result { get; set; }
        public string? Observations { get; set; }
    }
}