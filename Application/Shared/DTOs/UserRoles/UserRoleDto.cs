namespace Application.Shared.DTOs.UserRoles
{
    public record UserRoleDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdRole { get; set; }
    }
}
