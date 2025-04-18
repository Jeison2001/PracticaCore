namespace Domain.Entities
{
    public class UserPermission : BaseEntity<long>
    {
        public int IdUser { get; set; }
        public long IdPermission { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Relaciones
        public virtual User User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}