namespace Domain.Entities
{
    public class RolePermission : BaseEntity<int>
    {
        public int IdRole { get; set; }
        public int IdPermission { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Relaciones
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}