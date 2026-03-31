namespace Application.Shared.DTOs.TeachingAssignments
{
    public record TeachingAssignmentDto : BaseDto<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdTeacher { get; set; }
        public int IdTypeTeachingAssignment { get; set; }
        public DateTimeOffset? RevocationDate { get; set; }
        public int? IdTeacherResearchProfile { get; set; }
    }
}

