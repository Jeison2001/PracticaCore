namespace Domain.Common.Teachers
{
    /// <summary>
    /// Docente asignado a una inscripción (director o jurado), proyectado desde
    /// TeachingAssignment + User + TypeTeachingAssignment.
    /// </summary>
    public class AssignedTeacher
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
