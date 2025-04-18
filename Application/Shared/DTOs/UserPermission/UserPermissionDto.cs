namespace Application.Shared.DTOs.UserPermission
{
    public class UserPermissionDto : BaseDto<long>
    {
        public int IdUser { get; set; }
        public long IdPermission { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}