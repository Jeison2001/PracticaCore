namespace Infrastructure.Extensions
{
    /// <summary>
    /// Separa claves de filtro sobre Proposal que FilterBuilder no resuelve (no son
    /// propiedades directas de la entidad raíz del listado) del resto de filtros,
    /// que siguen su curso normal (FilterBuilder). Mismo patrón que
    /// TeacherAssignmentLoader.ExtractTeacherFilters para los docentes.
    /// </summary>
    public static class ProposalFilterLoader
    {
        /// <summary>
        /// Listados con raíz PreliminaryProject/ProjectFinal: extrae `title` y
        /// `researchline`, que viven en Proposal (comparte Id con la entidad raíz)
        /// y se traducen a EXISTS sobre la propuesta.
        /// </summary>
        public static ProposalFilterSet ExtractProjectFilters(Dictionary<string, string>? filters)
        {
            var set = new ProposalFilterSet();

            foreach (var f in filters ?? new Dictionary<string, string>())
            {
                var prop = f.Key.Contains('@') ? f.Key.Split('@', 2)[0] : f.Key;
                switch (prop.ToLowerInvariant())
                {
                    case "title": set.Title = f.Value; break;
                    case "researchline": set.ResearchLineName = f.Value; break;
                    default: set.Remaining[f.Key] = f.Value; break;
                }
            }

            return set;
        }

        /// <summary>
        /// Listado con raíz Proposal: extrae `researchlinename` y `statestagename`,
        /// que viven en navegaciones de Proposal. `title` NO se extrae: FilterBuilder
        /// lo resuelve como propiedad directa de Proposal.
        /// </summary>
        public static ProposalFilterSet ExtractProposalDetailFilters(Dictionary<string, string>? filters)
        {
            var set = new ProposalFilterSet();

            foreach (var f in filters ?? new Dictionary<string, string>())
            {
                var prop = f.Key.Contains('@') ? f.Key.Split('@', 2)[0] : f.Key;
                switch (prop.ToLowerInvariant())
                {
                    case "researchlinename": set.ResearchLineName = f.Value; break;
                    case "statestagename": set.StateStageName = f.Value; break;
                    default: set.Remaining[f.Key] = f.Value; break;
                }
            }

            return set;
        }

        /// <summary>Claves de Proposal extraídas de la petición (Remaining = el resto).</summary>
        public class ProposalFilterSet
        {
            public string? Title { get; set; }
            public string? ResearchLineName { get; set; }
            public string? StateStageName { get; set; }
            public Dictionary<string, string> Remaining { get; } = new();
        }
    }
}
