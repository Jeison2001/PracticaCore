namespace Application.Shared.DTOs.UserPermission
{
    public class UserPermissionDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdPermission { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}