namespace Domain.Entities
{
    public class UserRole : BaseEntity<int>
    {
        public int IdUser { get; set; }
        public int IdRole { get; set; }

        // Relaciones
        public virtual User User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
    }
}