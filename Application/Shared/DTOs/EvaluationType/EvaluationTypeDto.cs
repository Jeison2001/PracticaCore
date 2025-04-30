using Application.Shared.DTOs;

namespace Application.Shared.DTOs.EvaluationType
{
    public class EvaluationTypeDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}