namespace Application.Shared.DTOs.RolePermission
{
    public class RolePermissionDto : BaseDto<int>
    {
        public int IdRole { get; set; }
        public int IdPermission { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}