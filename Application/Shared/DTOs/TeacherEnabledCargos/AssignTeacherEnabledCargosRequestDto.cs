namespace Application.Shared.DTOs.TeacherEnabledCargos
{
    /// <summary>
    /// Request para asignar los cargos habilitados a un docente (lista completa: upsert semántico).
    /// </summary>
    public record AssignTeacherEnabledCargosRequestDto
    {
        public int UserId { get; init; }
        public List<int> TypeTeachingAssignmentIds { get; init; } = new();
    }
}
