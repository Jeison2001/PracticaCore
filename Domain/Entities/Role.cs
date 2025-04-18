namespace Domain.Entities
{
    public class Role : BaseEntity<int>
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? IdUserCreatedAt { get; set; }

        // Add this property to fix the error
        public virtual ICollection<UserRole> UserRole { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}