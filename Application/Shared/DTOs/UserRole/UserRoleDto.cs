namespace Application.Shared.DTOs.UserRole
{
    public class UserRoleDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdRole { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}