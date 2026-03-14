namespace Application.Shared.DTOs.UserPermissions
{
    public record UserPermissionDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdPermission { get; set; }
    }
}
