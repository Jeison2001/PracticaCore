using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
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

        Task<List<RequiredDocumentsByState>> GetRequiredDocumentsByCurrentStateAsync(
            int inscriptionModalityId,
            CancellationToken cancellationToken);
    }
}
