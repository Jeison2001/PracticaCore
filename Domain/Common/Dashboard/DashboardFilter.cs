namespace Domain.Common.Dashboard
{
    /// <summary>
    /// Filtros del dashboard de reportes. Todos opcionales y combinables.
    /// Las listas filtran por inclusión (IN); las fechas acotan por CreatedAt de la inscripción.
    /// </summary>
    public class DashboardFilter
    {
        public DateTimeOffset? DateFrom { get; set; }
        public DateTimeOffset? DateTo { get; set; }
        public List<string>? ModalityCodes { get; set; }
        public List<string>? StateCodes { get; set; }
        public List<int>? AcademicPeriodIds { get; set; }
        public List<int>? AcademicProgramIds { get; set; }
        public List<int>? FacultyIds { get; set; }

        // --- Filtros generales por conteo de participantes (aplican a todas las modalidades) ---
        /// <summary>Mínimo de estudiantes vinculados al registro (UserInscriptionModality activos).</summary>
        public int? MinStudents { get; set; }
        public int? MaxStudents { get; set; }
        /// <summary>Mínimo de docentes asignados (TeachingAssignment activos, sin revocar).</summary>
        public int? MinTeachers { get; set; }
        public int? MaxTeachers { get; set; }
        /// <summary>Filtra registros con (true) o sin (false) asignación docente. null = sin filtro.</summary>
        public bool? HasTeachingAssignment { get; set; }

        /// <summary>
        /// Filtros condicionados a la modalidad seleccionada. Solo se evalúan cuando
        /// <see cref="ModalityCodes"/> contiene EXACTAMENTE una modalidad; en otro caso se ignoran.
        /// </summary>
        public ModalityScopedFilter? ModalityScope { get; set; }
    }

    /// <summary>
    /// Filtros específicos de una modalidad (drill-down). Cada propiedad aplica a la entidad de
    /// extensión correspondiente; los estados de fase se resuelven por <c>Code</c> (nunca por Id).
    /// Solo tiene efecto cuando hay una única modalidad seleccionada en el filtro general.
    /// </summary>
    public class ModalityScopedFilter
    {
        // --- Comunes a modalidades multi-fase ---
        /// <summary>Fases por orden (StageModality.StageOrder) de la inscripción actual.</summary>
        public List<int>? StageOrders { get; set; }
        /// <summary>Estados de fase por Code (StateStage.Code) leídos de la entidad de extensión.</summary>
        public List<string>? StateStageCodes { get; set; }

        // --- PUBLICACION_ARTICULO ---
        public List<string>? JournalCategories { get; set; }
        public string? Issn { get; set; }
        public DateTimeOffset? AcceptanceFrom { get; set; }
        public DateTimeOffset? AcceptanceTo { get; set; }

        // --- Rangos numéricos reutilizables (semánticos por concepto) ---
        /// <summary>Promedio: GRADO_PROMEDIO.CertifiedAverage / COTERMINAL.FirstSemesterAverage.</summary>
        public decimal? MinAverage { get; set; }
        public decimal? MaxAverage { get; set; }
        /// <summary>Nota final: SEMINARIO_ACT.FinalGrade.</summary>
        public decimal? MinGrade { get; set; }
        public decimal? MaxGrade { get; set; }
        /// <summary>Asistencia: SEMINARIO_ACT.AttendancePercentage.</summary>
        public decimal? MinAttendance { get; set; }
        public decimal? MaxAttendance { get; set; }
        /// <summary>Puntaje: SABER_PRO.ResultScore.</summary>
        public decimal? MinScore { get; set; }
        public decimal? MaxScore { get; set; }
        /// <summary>Horas de práctica: PRACTICA_ACADEMICA.PracticeHours.</summary>
        public int? MinHours { get; set; }
        public int? MaxHours { get; set; }

        // --- Booleanos (tri-state: null = sin filtro) ---
        /// <summary>GRADO_PROMEDIO.HasFailedSubjects.</summary>
        public bool? HasFailedSubjects { get; set; }
        /// <summary>PRACTICA_ACADEMICA.IsEmprendimiento.</summary>
        public bool? IsEmprendimiento { get; set; }

        // --- Discretos ---
        /// <summary>SABER_PRO.ResultQuintile (p.ej. Q3, Q4, Q5).</summary>
        public List<string>? ResultQuintiles { get; set; }
        /// <summary>PROYECTO_GRADO: líneas de investigación de la propuesta (Proposal.IdResearchLine).</summary>
        public List<int>? ResearchLineIds { get; set; }
    }
}
