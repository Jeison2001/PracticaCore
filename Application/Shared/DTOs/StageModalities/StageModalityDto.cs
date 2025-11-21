namespace Application.Shared.DTOs.StageModalities
{
    public record StageModalityDto : BaseDto<int>
    {
        public int IdModality { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StageOrder { get; set; }
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}
