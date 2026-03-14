namespace Domain.Entities
{
    public class Permission : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public string Description { get; set; } = string.Empty;

        // Relacion virtual para representar jerarquia
        public virtual Permission? ParentPermission { get; set; }
        public virtual ICollection<Permission>? ChildPermissions { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}