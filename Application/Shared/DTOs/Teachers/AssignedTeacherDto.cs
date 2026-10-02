namespace Application.Shared.DTOs.Teachers
{
    /// <summary>Docente asignado (director/jurado) expuesto al cliente para mostrar y filtrar.</summary>
    public record AssignedTeacherDto
    {
        public int Id { get; set; }
        public int IdTeacher { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>Id del cargo (TypeTeachingAssignment).</summary>
        public int CargoId { get; set; }
        /// <summary>Código del cargo: DIRECTOR, CO_DIRECTOR, ASESOR, JURADO_EVALUADOR.</summary>
        public string? CargoCode { get; set; }
        /// <summary>Nombre del cargo: Director, Co-Director, Asesor, Jurado Evaluador.</summary>
        public string? CargoName { get; set; }
        public DateTimeOffset? RevocationDate { get; set; }
    }
}
