namespace Domain.Entities
{
    public class AcademicProgram : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }

        // Relación con Faculty
        public int IdFaculty { get; set; }
        public virtual Faculty Faculty { get; set; } = null!;
    }
}