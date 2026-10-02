using Domain.Entities;

namespace Domain.Common.Inscriptions
{
    /// <summary>
    /// Proyección de una inscripción a modalidad con sus datos relacionados y
    /// estudiantes (una fila de página del listado de gestión).
    /// </summary>
    public class InscriptionWithStudents
    {
        public required InscriptionModality InscriptionModality { get; set; }
        public required string AcademicPeriodCode { get; set; }
        public required string ModalityName { get; set; }
        public required string StateInscriptionName { get; set; }
        public string? StateInscriptionCode { get; set; }
        public string? StageModalityName { get; set; }
        public int? StageOrder { get; set; }
        /// <summary>Estudiantes de la inscripción, con su navegación User cargada.</summary>
        public required List<UserInscriptionModality> Students { get; set; }
    }
}
