namespace Domain.Entities
{
    public class UserRole : BaseEntity<int>
    {
        public int IdUser { get; set; }
        public int IdRole { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Relaciones
        public virtual User User { get; set; }
        public virtual Role Role { get; set; }
    }
}