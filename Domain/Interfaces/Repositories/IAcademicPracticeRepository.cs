using Domain.Common;
using Domain.Common.AcademicPractice;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IAcademicPracticeRepository : IRepository<AcademicPractice, int>, IScopedService
    {
        /// <summary>
        /// Obtiene prácticas académicas asignadas a un profesor a través de la entidad TeachingAssignment con paginación
        /// </summary>
        /// <param name="teacherId">ID del profesor</param>
        /// <param name="pageNumber">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="sortBy">Campo por el cual ordenar</param>
        /// <param name="isDescending">Indica si el ordenamiento es descendente</param>
        /// <param name="filters">Filtros adicionales que pueden incluir el estado</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado paginado de prácticas académicas con todos sus detalles relacionados</returns>
        Task<PaginatedResult<AcademicPracticeWithDetails>> GetAcademicPracticesByTeacherWithDetailsPaginatedAsync(
            int teacherId,
            int pageNumber, 
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene prácticas académicas con sus detalles para un usuario, evitando operaciones paralelas en el DbContext
        /// </summary>
        Task<List<AcademicPracticeWithDetails>> GetAcademicPracticesByUserWithDetailsAsync(
            int userId,
            bool? status = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todas las prácticas académicas con paginación y detalles completos
        /// </summary>
        Task<PaginatedResult<AcademicPracticeWithDetails>> GetAllAcademicPracticesWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string sortBy,
            bool isDescending, 
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene una práctica académica con todos sus detalles
        /// </summary>
        Task<AcademicPracticeWithDetails?> GetAcademicPracticeWithDetailsAsync(
            int id, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza el estado de una práctica académica y maneja las fechas de aprobación automáticamente
        /// </summary>
        Task<bool> UpdateAcademicPracticeStateAsync(
            int id, 
            int newStateStageId, 
            string? observations = null,
            string? evaluatorObservations = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el progreso de fases de una práctica académica
        /// </summary>
        Task<AcademicPracticePhaseProgress> GetPhaseProgressAsync(
            int id, 
            CancellationToken cancellationToken = default);
    }
}
