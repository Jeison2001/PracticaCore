namespace Application.Shared.DTOs.UserRoles
{
    public class UserRoleDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdRole { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}