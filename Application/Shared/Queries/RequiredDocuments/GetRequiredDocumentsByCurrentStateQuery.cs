using Application.Shared.DTOs;
using MediatR;

namespace Application.Shared.Queries
{
    public class GetRequiredDocumentsByCurrentStateQuery : IRequest<List<RequiredDocumentDto>>
    {
        public int InscriptionModalityId { get; set; }

        public GetRequiredDocumentsByCurrentStateQuery(int inscriptionModalityId)
        {
            InscriptionModalityId = inscriptionModalityId;
        }
    }
}
