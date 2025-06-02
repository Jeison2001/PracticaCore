namespace Domain.Entities
{
    public class StateStage : BaseEntity<int>
    {
        public int IdStageModality { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsInitialState { get; set; } = false;
        public bool IsFinalStateForStage { get; set; } = false;
        public bool IsFinalStateForModalityOverall { get; set; } = false;
        public new int? IdUserCreatedAt { get; set; }

        // Navigation properties
        public virtual StageModality? StageModality { get; set; }
    }
}
