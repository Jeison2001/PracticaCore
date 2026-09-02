namespace Application.Shared.DTOs.TeachingAssignments
{
    /// <summary>
    /// Agrupa las asignaciones de un docente bajo un tipo de cargo específico (Director, Co-Director, Asesor, Jurado, etc.).
    /// </summary>
    public record TeacherCargoGroupDto
    {
        public int IdTypeTeachingAssignment { get; init; }
        public string CargoCode { get; init; } = string.Empty;
        public string CargoName { get; init; } = string.Empty;
        public int? MaxAssignments { get; init; }
        public int ActiveAssignmentsCount { get; init; }
        public List<TeacherAssignedProjectDto> Assignments { get; init; } = new();
    }
}
