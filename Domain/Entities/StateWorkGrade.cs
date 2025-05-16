namespace Domain.Entities
{
    public class StateWorkGrade : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        // Relación con InscriptionModality
        public virtual ICollection<InscriptionModality>? InscriptionModalities { get; set; }
    }
}