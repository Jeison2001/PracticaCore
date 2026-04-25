namespace Domain.Entities
{
    public class StudyPlan : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int IdAcademicProgram { get; set; }
        public int StartYear { get; set; }
        public int? EndYear { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual AcademicProgram AcademicProgram { get; set; } = null!;
    }
}
