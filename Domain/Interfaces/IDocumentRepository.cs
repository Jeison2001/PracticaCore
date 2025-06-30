using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Registration;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IDocumentRepository : IScopedService
    {
        Task<PaginatedResult<Document>> GetDocumentsByInscriptionModalityWithFiltersAsync(
            int idInscriptionModality,
            int? idStageModality,
            int? idDocumentClass,
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool isDescending,
            CancellationToken cancellationToken);
    }
}
