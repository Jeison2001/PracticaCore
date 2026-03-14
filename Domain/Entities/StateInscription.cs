namespace Domain.Entities
{
    public class StateInscription : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsInitialState { get; set; } = false;
        public bool IsFinalStateForStage { get; set; } = false;
    }
}