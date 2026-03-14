using Domain.Entities;

namespace Domain.Entities
{
    public class CoTerminal : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public string? PostgraduateProgramName { get; set; }
        public string? UniversityName { get; set; }
        public decimal? FirstSemesterAverage { get; set; }
        public string? Observations { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}
