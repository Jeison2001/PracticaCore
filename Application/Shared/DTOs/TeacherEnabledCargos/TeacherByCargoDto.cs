namespace Application.Shared.DTOs.TeacherEnabledCargos
{
    public record TeacherByCargoDto
    {
        public int IdUser { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Identification { get; init; } = string.Empty;
        public int IdTypeTeachingAssignment { get; init; }
    }
}
