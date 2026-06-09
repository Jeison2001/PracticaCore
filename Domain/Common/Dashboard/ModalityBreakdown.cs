namespace Domain.Common.Dashboard
{
    /// <summary>Punto de un embudo de fases: fase (orden+code+nombre) y conteo en ella.</summary>
    public record PhasePoint(int StageOrder, string StageCode, string StageName, int Count);

    /// <summary>
    /// Métricas de drill-down para UNA modalidad seleccionada. Se calcula solo cuando el filtro
    /// trae exactamente una modalidad. Los conteos respetan todos los filtros (generales + scope).
    /// </summary>
    public class ModalityBreakdown
    {
        public string ModalityCode { get; set; } = "";
        public string ModalityName { get; set; } = "";

        /// <summary>Total de inscripciones de la modalidad bajo el filtro actual.</summary>
        public int Total { get; set; }

        /// <summary>Distribución por estado de fase (StateStage) leído de la entidad de extensión.</summary>
        public List<CategoryCount> ByStateStage { get; set; } = new();

        /// <summary>Embudo por fases: cuántas inscripciones están actualmente en cada fase.</summary>
        public List<PhasePoint> PhaseFunnel { get; set; } = new();

        /// <summary>
        /// Distribución por una dimensión propia de la modalidad (JournalCategory, ResultQuintile,
        /// HasFailedSubjects, IsEmprendimiento, línea de investigación). Vacío si no aplica.
        /// </summary>
        public List<CategoryCount> Distribution { get; set; } = new();
        /// <summary>Etiqueta de la dimensión de <see cref="Distribution"/> (para el título del gráfico).</summary>
        public string? DistributionLabel { get; set; }

        /// <summary>Histograma de un campo numérico relevante (horas, promedio, puntaje, nota). Vacío si no aplica.</summary>
        public List<CategoryCount> NumericBuckets { get; set; } = new();
        /// <summary>Etiqueta del histograma numérico.</summary>
        public string? NumericLabel { get; set; }

        /// <summary>Asignaciones docentes por tipo (solo PROYECTO_GRADO y PRACTICA_ACADEMICA). Vacío si no aplica.</summary>
        public List<CategoryCount> AssignmentsByType { get; set; } = new();
    }
}
