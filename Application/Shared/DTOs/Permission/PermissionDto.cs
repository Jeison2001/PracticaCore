namespace Application.Shared.DTOs.Permission
{
    public class PermissionDto : BaseDto<long>
    {
        public string Code { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}