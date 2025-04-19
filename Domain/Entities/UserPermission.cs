namespace Domain.Entities
{
    public class UserPermission : BaseEntity<int>
    {
        public int IdUser { get; set; }
        public int IdPermission { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Relaciones
        public virtual User User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}