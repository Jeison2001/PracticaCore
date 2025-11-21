namespace Application.Shared.DTOs.IdentificationTypes
{
    public record IdentificationTypeDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }
    }
}
