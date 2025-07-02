using Application.Shared.DTOs.RequiredDocumentsByState;
using Application.Shared.Queries.RequiredDocuments;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.RequiredDocuments.Handlers
{
    public class GetRequiredDocumentsByCurrentStateHandler : IRequestHandler<GetRequiredDocumentsByCurrentStateQuery, List<RequiredDocumentsByStateDto>>
    {
        private readonly IDocumentRepository _documentRepository;

        public GetRequiredDocumentsByCurrentStateHandler(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<List<RequiredDocumentsByStateDto>> Handle(GetRequiredDocumentsByCurrentStateQuery request, CancellationToken cancellationToken)
        {
            var requiredDocumentsEntities = await _documentRepository.GetRequiredDocumentsByCurrentStateAsync(
                request.InscriptionModalityId, 
                cancellationToken);

            // Mapear las entidades a DTOs, incluyendo los campos heredados
            var requiredDocuments = requiredDocumentsEntities.Select(rds => new RequiredDocumentsByStateDto
            {
                Id = rds.Id,
                IdUserCreatedAt = rds.IdUserCreatedAt,
                CreatedAt = rds.CreatedAt,
                IdUserUpdatedAt = rds.IdUserUpdatedAt,
                UpdatedAt = rds.UpdatedAt,
                OperationRegister = rds.OperationRegister,
                StatusRegister = rds.StatusRegister,

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
