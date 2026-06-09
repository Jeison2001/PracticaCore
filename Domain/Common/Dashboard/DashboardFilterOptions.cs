namespace Domain.Common.Dashboard
{
    /// <summary>Opción de filtro (id/code + etiqueta legible) para poblar los selects del dashboard.</summary>
    public record FilterOption(string Value, string Label);

    /// <summary>Fase de una modalidad (para filtros condicionados): orden + code + nombre.</summary>
    public record StageOption(int StageOrder, string Code, string Name);

    /// <summary>Estado de fase de una modalidad: code + nombre + a qué fase (StageOrder) pertenece.</summary>
    public record StateStageOption(string Code, string Name, int StageOrder, string StageCode);

    /// <summary>
    /// Metadata de filtros condicionados disponibles para UNA modalidad. El frontend la usa para
    /// pintar dinámicamente los filtros sin hardcodear codes ni listas.
    /// </summary>
    public class ModalityFilterMeta
    {
        public string ModalityCode { get; set; } = "";
        public string ModalityName { get; set; } = "";

        /// <summary>Fases ordenadas (solo presente en modalidades multi-fase).</summary>
        public List<StageOption> Stages { get; set; } = new();
        /// <summary>Estados de fase disponibles para esta modalidad.</summary>
        public List<StateStageOption> StateStages { get; set; } = new();

        /// <summary>Qué filtros condicionados aplican a esta modalidad (claves estables para el FE).</summary>
        public List<string> AvailableFilters { get; set; } = new();

        /// <summary>Valores discretos disponibles (distinct) cuando aplica: categorías de revista, quintiles, etc.</summary>
        public List<string> JournalCategories { get; set; } = new();
        public List<string> ResultQuintiles { get; set; } = new();
        public List<FilterOption> ResearchLines { get; set; } = new();
    }

    /// <summary>Catálogo de opciones disponibles para los filtros del dashboard.</summary>
    public class DashboardFilterOptions
    {
        public List<FilterOption> Modalities { get; set; } = new();
        public List<FilterOption> States { get; set; } = new();
        public List<FilterOption> AcademicPeriods { get; set; } = new();
        public List<FilterOption> Programs { get; set; } = new();
        public List<FilterOption> Faculties { get; set; } = new();

        /// <summary>Metadata de filtros condicionados, indexada por code de modalidad.</summary>
        public List<ModalityFilterMeta> ModalityFilters { get; set; } = new();
    }
}
