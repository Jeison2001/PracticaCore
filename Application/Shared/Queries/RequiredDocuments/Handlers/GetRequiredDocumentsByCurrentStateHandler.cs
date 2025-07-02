using Application.Shared.DTOs;
using Application.Shared.Queries;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.Handlers
{
    public class GetRequiredDocumentsByCurrentStateHandler : IRequestHandler<GetRequiredDocumentsByCurrentStateQuery, List<RequiredDocumentDto>>
    {
        private readonly IDocumentRepository _documentRepository;

        public GetRequiredDocumentsByCurrentStateHandler(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<List<RequiredDocumentDto>> Handle(GetRequiredDocumentsByCurrentStateQuery request, CancellationToken cancellationToken)
        {
            var requiredDocumentsEntities = await _documentRepository.GetRequiredDocumentsByCurrentStateAsync(
                request.InscriptionModalityId, 
                cancellationToken);

            // Mapear las entidades a DTOs
            var requiredDocuments = requiredDocumentsEntities.Select(rds => new RequiredDocumentDto
            {
                DocumentTypeId = rds.DocumentType.Id,
                DocumentCode = rds.DocumentType.Code,
                DocumentName = rds.DocumentType.Name,
                Description = rds.DocumentType.Description,
                IsRequired = rds.IsRequired,
                OrderDisplay = rds.OrderDisplay,
                DocumentClassName = rds.DocumentType.DocumentClass.Name,
                RequiredForState = rds.StateStage.Code,
                StateName = rds.StateStage.Name
            }).ToList();

            return requiredDocuments;
        }
    }
}
