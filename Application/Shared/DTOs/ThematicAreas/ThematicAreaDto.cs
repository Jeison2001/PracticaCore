namespace Application.Shared.DTOs.ThematicAreas
{
    public class ThematicAreaDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        public int IdResearchSubLine { get; set; }
    }
}