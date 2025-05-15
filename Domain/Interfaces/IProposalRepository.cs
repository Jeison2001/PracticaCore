using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Registration;

namespace Domain.Interfaces
{
    public interface IProposalRepository : IRepository<Proposal, int>, IScopedService
    {
        /// <summary>
        /// Obtiene propuestas asignadas a un profesor a través de la entidad TeachingAssignment con paginación
        /// </summary>
        /// <param name="teacherId">ID del profesor</param>
        /// <param name="pageNumber">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="sortBy">Campo por el cual ordenar</param>
        /// <param name="isDescending">Indica si el ordenamiento es descendente</param>
        /// <param name="filters">Filtros adicionales que pueden incluir el estado</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado paginado de propuestas con todos sus detalles relacionados</returns>
        Task<PaginatedResult<ProposalWithDetails>> GetProposalsByTeacherWithDetailsPaginatedAsync(
            int teacherId,
            int pageNumber, 
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default);/// <summary>
        /// Obtiene propuestas con sus detalles para un usuario, evitando operaciones paralelas en el DbContext
        /// </summary>
        Task<List<ProposalWithDetails>> GetProposalsByUserWithDetailsAsync(
            int userId,
            bool? status = null,
            CancellationToken cancellationToken = default);            
        /// <summary>
        /// Obtiene una propuesta específica por ID con todos sus detalles relacionados
        /// </summary>
        /// <param name="id">ID de la propuesta</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Propuesta con todos sus detalles relacionados</returns>
        Task<ProposalWithDetails?> GetProposalWithDetailsAsync(
            int id,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Obtiene todas las propuestas con paginación, filtrado y ordenamiento junto con sus detalles relacionados
        /// </summary>
        /// <param name="pageNumber">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="sortBy">Campo por el cual ordenar</param>
        /// <param name="isDescending">Indica si el ordenamiento es descendente</param>
        /// <param name="filters">Filtros adicionales</param>
        /// <param name="status">Estado opcional para filtrar resultados</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado paginado de propuestas con todos sus detalles relacionados</returns>
        Task<PaginatedResult<ProposalWithDetails>> GetAllProposalsWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            bool? status = null,
            CancellationToken cancellationToken = default);
    }
}