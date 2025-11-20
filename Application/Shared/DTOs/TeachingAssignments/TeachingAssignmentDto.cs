namespace Application.Shared.DTOs.TeachingAssignments
{
    public class TeachingAssignmentDto : BaseDto<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdTeacher { get; set; }
        public int IdTypeTeachingAssignment { get; set; }
        public DateTime? RevocationDate { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        public int? IdTeacherResearchProfile { get; set; }
    }
}