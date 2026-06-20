namespace Domain.Entities
{
    public class StageModality : BaseEntity<int>
    {
        public int IdModality { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StageOrder { get; set; }
        public string? Description { get; set; }
        // Navigation properties
        public virtual Modality Modality { get; set; } = null!;
        public virtual ICollection<StateStage> StateStages { get; set; } = new List<StateStage>();
        public virtual ICollection<InscriptionModality> InscriptionModalities { get; set; } = new List<InscriptionModality>();
    }
}
