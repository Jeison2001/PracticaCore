namespace Application.Shared.DTOs.EvaluationsTypes
{
    public class EvaluationTypeDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}