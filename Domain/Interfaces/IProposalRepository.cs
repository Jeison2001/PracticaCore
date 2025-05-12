using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Registration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IProposalRepository : IRepository<Proposal, int>, IScopedService
    {
        /// <summary>
        /// Obtiene propuestas asignadas a un profesor a través de la entidad TeachingAssignment
        /// </summary>
        /// <param name="teacherId">ID del profesor</param>
        /// <param name="status">Estado opcional para filtrar resultados</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de propuestas con todos sus detalles relacionados</returns>
        Task<List<ProposalWithDetails>> GetProposalsByTeacherWithDetailsAsync(
            int teacherId, 
            bool? status = null,
            CancellationToken cancellationToken = default);
    }
}