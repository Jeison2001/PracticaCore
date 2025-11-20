namespace Application.Shared.DTOs.Evaluations
{
    public class EvaluationDto : BaseDto<int>
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int IdEvaluationType { get; set; }
        public int IdEvaluator { get; set; }
        public string? Result { get; set; }
        public string? Observations { get; set; }
    }
}