namespace Domain.Entities
{
    public class Role : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }

        // Add this property to fix the error
        public virtual ICollection<UserRole> UserRole { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}