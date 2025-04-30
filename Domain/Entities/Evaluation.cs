namespace Domain.Entities
{
    public class Evaluation : BaseEntity<int>
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int IdEvaluationType { get; set; }
        public int IdEvaluator { get; set; }
        public string? Result { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        
        // Navigation properties
        public virtual EvaluationType EvaluationType { get; set; } = null!;
        public virtual User Evaluator { get; set; } = null!;
    }
}