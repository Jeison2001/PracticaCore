namespace Domain.Entities
{
    public class EvaluationType : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Navigation property
        public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}