using Domain.Common;
using Domain.Common.Inscriptions;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    /// <summary>
    /// Listado paginado de inscripciones a modalidad con estudiantes y datos relacionados,
    /// con filtrado en base de datos (incluye claves enriquecidas traducidas a joins).
    /// </summary>
    public interface IInscriptionWithStudentsRepository : IRepository<InscriptionModality, int>, IScopedService
    {
        /// <summary>
        /// Obtiene una página de inscripciones con sus estudiantes aplicando los filtros en SQL.
        /// Claves enriquecidas soportadas (formato Filters[Clave@op]=valor):
        /// StudentName@like, ModalityName@like, StateInscriptionName@eq, AcademicPeriodCode@eq.
        /// El resto de claves se evalúan contra propiedades de InscriptionModality (FilterBuilder).
        /// </summary>
        Task<PaginatedResult<InscriptionWithStudents>> GetFilteredPageAsync(
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool isDescending,
            Dictionary<string, string>? filters,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false);
    }
}
