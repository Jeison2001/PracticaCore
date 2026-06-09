namespace Domain.Common.Dashboard
{
    /// <summary>Par etiqueta/valor para series de gráficos y conteos agrupados.</summary>
    public record CategoryCount(string Key, string Label, int Count);

    /// <summary>Punto de serie temporal (yyyy-MM) con su conteo.</summary>
    public record TimePoint(string Period, int Count);

    /// <summary>Conteo cruzado modalidad × estado para gráficos apilados.</summary>
    public record ModalityStateCount(string ModalityCode, string ModalityName, string StateCode, int Count);

    /// <summary>
    /// Resultado agregado del dashboard según los filtros aplicados.
    /// </summary>
    public class DashboardSummary
    {
        // KPIs
        public int TotalInscriptions { get; set; }
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int NotApplicable { get; set; }
        public double ApprovalRate { get; set; }

        // Breakdowns (para gráficos)
        public List<CategoryCount> ByModality { get; set; } = new();
        public List<CategoryCount> ByState { get; set; } = new();
        public List<CategoryCount> ByAcademicPeriod { get; set; } = new();
        public List<CategoryCount> ByProgram { get; set; } = new();
        public List<CategoryCount> ByFaculty { get; set; } = new();
        public List<TimePoint> Timeline { get; set; } = new();
        public List<ModalityStateCount> ModalityByState { get; set; } = new();

        /// <summary>Distribución de registros por número de estudiantes (1 = individual, 2+ = grupal).</summary>
        public List<CategoryCount> ByStudentCount { get; set; } = new();

        /// <summary>Top docentes por número de asignaciones activas (nombre del docente).</summary>
        public List<CategoryCount> ByTeacherLoad { get; set; } = new();

        /// <summary>Asignaciones docentes activas agrupadas por tipo (Director, Co-director, Asesor, Jurado).</summary>
        public List<CategoryCount> ByAssignmentType { get; set; } = new();
    }
}
