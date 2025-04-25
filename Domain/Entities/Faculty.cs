namespace Domain.Entities
{
    public class Faculty : BaseEntity<int>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        public virtual ICollection<AcademicProgram> AcademicPrograms { get; set; } = new List<AcademicProgram>();
    }
}
