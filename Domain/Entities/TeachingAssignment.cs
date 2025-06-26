namespace Domain.Entities
{
    public class TeachingAssignment : BaseEntity<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdTeacher { get; set; }  // FK to user
        public int IdTypeTeachingAssignment { get; set; }
        public DateTime? RevocationDate { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        public int? IdTeacherResearchProfile { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual User Teacher { get; set; } = null!;
        public virtual TypeTeachingAssignment TypeTeachingAssignment { get; set; } = null!;
        public virtual TeacherResearchProfile? TeacherResearchProfile { get; set; }
    }
}