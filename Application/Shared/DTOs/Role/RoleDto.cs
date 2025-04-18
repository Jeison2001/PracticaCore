namespace Application.Shared.DTOs.Role
{
    public class RoleDto : BaseDto<int>
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}