namespace Domain.Entities
{
    public class StateProjectFinal : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsFinalState { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}