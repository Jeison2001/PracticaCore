namespace Application.Shared.DTOs.RolePermission
{
    public class RolePermissionDto : BaseDto<long>
    {
        public int IdRole { get; set; }
        public long IdPermission { get; set; }
        public new int? IdUserCreatedAt { get; set; }

    }
}