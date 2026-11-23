namespace Domain.Common.Dashboard
{
    /// <summary>Par etiqueta/valor para series de gráficos y conteos agrupados.</summary>
    public record CategoryCount(string Key, string Label, int Count);

    /// <summary>Punto de serie temporal (yyyy-MM) con su conteo.</summary>
    public record TimePoint(string Period, int Count);

    /// <summary>Conteo cruzado modalidad × estado de inscripción para gráficos apilados.</summary>
    public record ModalityStateCount(string ModalityCode, string ModalityName, string StateCode, int Count, string StateName = "");

    /// <summary>Conteo cruzado modalidad × estado consolidado de ciclo de vida.</summary>
    public record ModalityLifecycleCount(string ModalityCode, string ModalityName, string LifecycleCode, string LifecycleLabel, int Count);

    /// <summary>Conteo cruzado periodo académico × modalidad.</summary>
    public record PeriodModalityCount(string PeriodCode, string ModalityCode, string ModalityName, int Count);

    /// <summary>Carga docente detallada con desglose por rol de asignación.</summary>
    public record TeacherLoadDetail(int TeacherId, string TeacherName, int Total, Dictionary<string, int> ByRole);

    /// <summary>
    /// Resultado agregado del dashboard según los filtros aplicados.
    /// </summary>
    public class DashboardSummary
    {
        // KPIs de inscripción (compatibilidad)
        public int TotalInscriptions { get; set; }
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int NotApplicable { get; set; }
        public double ApprovalRate { get; set; }

        // KPIs consolidados de ciclo de vida (mutuamente excluyentes y exhaustivos)
        public int Completed { get; set; }
        public int InReview { get; set; }
        public int WithObservations { get; set; }
        public int InProgress { get; set; }
        public int RejectedOverall { get; set; }
        public double CompletionRate { get; set; }
        public double? SuccessRateOverClosed { get; set; }
        public double? AvgCycleDays { get; set; }
        public int StaleInscriptionsCount { get; set; }
        public int UpcomingDefensesCount { get; set; }

        // Breakdowns (para gráficos)
        public List<CategoryCount> ByModality { get; set; } = new();
        public List<CategoryCount> ByState { get; set; } = new();
        public List<CategoryCount> ByLifecycleStatus { get; set; } = new();
        public List<CategoryCount> ByAcademicPeriod { get; set; } = new();
        public List<CategoryCount> ByProgram { get; set; } = new();
        public List<CategoryCount> ByFaculty { get; set; } = new();
        public List<TimePoint> Timeline { get; set; } = new();
        public List<ModalityStateCount> ModalityByState { get; set; } = new();
        public List<ModalityLifecycleCount> ModalityByLifecycle { get; set; } = new();
        public List<PeriodModalityCount> PeriodByModality { get; set; } = new();

        /// <summary>Distribución de registros por número de estudiantes (1 = individual, 2+ = grupal).</summary>
        public List<CategoryCount> ByStudentCount { get; set; } = new();

        /// <summary>Top docentes por número de asignaciones activas (nombre del docente).</summary>
        public List<CategoryCount> ByTeacherLoad { get; set; } = new();

        /// <summary>Top docentes con desglose por tipo de rol (Director, Co-director, Asesor, Jurado Evaluador).</summary>
        public List<TeacherLoadDetail> TeacherLoadDetailed { get; set; } = new();

        /// <summary>Asignaciones docentes activas agrupadas por tipo (Director, Co-director, Asesor, Jurado).</summary>
        public List<CategoryCount> ByAssignmentType { get; set; } = new();
    }
}
