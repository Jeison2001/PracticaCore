using Domain.Entities;

namespace Domain.Entities
{
    public class AcademicAverage : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public decimal? CertifiedAverage { get; set; }
        public bool? HasFailedSubjects { get; set; }
        public string? Observations { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}
