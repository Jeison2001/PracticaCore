using Domain.Entities;

namespace Domain.Entities
{
    public class Seminar : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public string? SeminarName { get; set; }
        public decimal? AttendancePercentage { get; set; }
        public decimal? FinalGrade { get; set; }
        public string? Observations { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}
