namespace Domain.Common.Dashboard
{
    /// <summary>Punto de un embudo de fases: fase (orden+code+nombre) y conteo en ella.</summary>
    public record PhasePoint(int StageOrder, string StageCode, string StageName, int Count);

    /// <summary>Estadísticas descriptivas de una variable numérica (horas, puntaje, promedio, nota).</summary>
    public record NumericSummaryStats(int Count, decimal Min, decimal Avg, decimal Median, decimal Max, string Unit);

    /// <summary>Resumen de sustentaciones de Proyecto de Grado (ProjectPresentation).</summary>
    public record DefenseMetricsSummary(
        int TotalScheduled,
        int ApprovedFirstAttempt,
        int ApprovedSecondAttempt,
        int RetryPending,
        int RejectedFinal,
        List<CategoryCount> ByModality);

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

        /// <summary>Distribución por estado de fase efectivo (StateStage) leído de la entidad de extensión activa.</summary>
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
        /// <summary>Estadísticas descriptivas (Count, Min, Avg, Median, Max) de la variable numérica.</summary>
        public NumericSummaryStats? NumericStats { get; set; }

        /// <summary>Top entidades/revistas/sublíneas/seminarios/programas según modalidad (Top 10).</summary>
        public List<CategoryCount> TopEntities { get; set; } = new();
        /// <summary>Etiqueta de <see cref="TopEntities"/>.</summary>
        public string? TopEntitiesLabel { get; set; }

        /// <summary>Métricas de sustentaciones (solo aplica a PROYECTO_GRADO).</summary>
        public DefenseMetricsSummary? DefenseSummary { get; set; }

        /// <summary>Asignaciones docentes por tipo (solo PROYECTO_GRADO y PRACTICA_ACADEMICA). Vacío si no aplica.</summary>
        public List<CategoryCount> AssignmentsByType { get; set; } = new();
    }
}
