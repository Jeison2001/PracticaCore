namespace Application.Shared.DTOs.Permissions
{
    public record PermissionDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public string Description { get; set; } = string.Empty;

    }
}
