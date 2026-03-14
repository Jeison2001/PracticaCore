using Domain.Entities;

namespace Domain.Entities
{
    public class SaberPro : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ExamDate { get; set; }
        public string? ResultQuintile { get; set; }
        public decimal? ResultScore { get; set; }
        public string? Observations { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}
