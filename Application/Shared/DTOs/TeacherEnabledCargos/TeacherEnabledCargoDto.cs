namespace Application.Shared.DTOs.TeacherEnabledCargos
{
    /// <summary>
    /// Cargo habilitado para un docente (relación TeacherEnabledCargo).
    /// </summary>
    public record TeacherEnabledCargoDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdTypeTeachingAssignment { get; set; }

        // Datos del cargo para mostrar en UI
        public string? CargoCode { get; set; }
        public string? CargoName { get; set; }
        public int? MaxAssignments { get; set; }
    }
}
