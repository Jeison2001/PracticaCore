namespace Application.Shared.DTOs.ThematicAreas
{
    public record ThematicAreaDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int IdResearchSubLine { get; set; }
    }
}
