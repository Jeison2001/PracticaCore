namespace Domain.Entities
{
    public class StateProposal : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        
        // Navigation property
        public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
    }
}