namespace Application.Shared.DTOs.Permissions
{
    public class PermissionDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }

    }
}