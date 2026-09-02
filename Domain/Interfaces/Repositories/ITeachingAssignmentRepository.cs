using Domain.Common.TeachingAssignments;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface ITeachingAssignmentRepository : IRepository<TeachingAssignment, int>, IScopedService
    {
        /// <summary>
        /// Obtiene las asignaciones de docentes para una propuesta específica con sus detalles relacionados
        /// </summary>
        /// <param name="proposalId">ID de la propuesta/inscripción</param>
        /// <param name="statusRegister">Estado opcional para filtrar resultados</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de asignaciones con detalles del docente y tipo de asignación</returns>
        Task<List<TeachingAssignmentWithDetails>> GetTeachingAssignmentsByProposalWithDetailsAsync(
            int proposalId,
            bool? statusRegister = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene las asignaciones de un docente para un cargo específico con detalles de modalidad y estado.
        /// </summary>
        Task<List<TeachingAssignment>> GetAssignmentsByTeacherAndCargoAsync(
            int teacherId,
            int cargoId,
            bool includeRevoked = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene las asignaciones de un docente con detalles de modalidad y estado, opcionalmente filtradas por cargo.
        /// </summary>
        Task<List<TeachingAssignment>> GetAssignmentsByTeacherAsync(
            int teacherId,
            int? cargoId = null,
            bool includeRevoked = false,
            CancellationToken cancellationToken = default);
    }
}
