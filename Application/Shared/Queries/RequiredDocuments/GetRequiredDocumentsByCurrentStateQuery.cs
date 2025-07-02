using Application.Shared.DTOs.RequiredDocumentsByState;
using MediatR;

namespace Application.Shared.Queries.RequiredDocuments
{
    public class GetRequiredDocumentsByCurrentStateQuery : IRequest<List<RequiredDocumentsByStateDto>>
    {
        public int InscriptionModalityId { get; set; }

        public GetRequiredDocumentsByCurrentStateQuery(int inscriptionModalityId)
        {
            InscriptionModalityId = inscriptionModalityId;
        }
    }
}
