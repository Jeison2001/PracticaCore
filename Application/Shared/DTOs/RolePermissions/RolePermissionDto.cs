namespace Application.Shared.DTOs.RolePermissions
{
    public record RolePermissionDto : BaseDto<int>
    {
        public int IdRole { get; set; }
        public int IdPermission { get; set; }
    }
}
