namespace Domain.Entities
{
    public class StageModality : BaseEntity<int>
    {
        public int IdModality { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StageOrder { get; set; }
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Navigation properties
        public virtual Modality? Modality { get; set; }
        public virtual ICollection<StateStage>? StateStages { get; set; }
    }
}
