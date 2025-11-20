namespace Domain.Entities
{
    public class Faculty : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }
        public virtual ICollection<AcademicProgram> AcademicPrograms { get; set; } = new List<AcademicProgram>();
    }
}
