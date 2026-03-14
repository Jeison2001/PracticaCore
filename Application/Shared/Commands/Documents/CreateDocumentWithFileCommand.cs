using Application.Shared.DTOs.Documents;
using MediatR;

namespace Application.Shared.Commands.Documents
{
    public record CreateDocumentWithFileCommand : IRequest<DocumentDto>
    {
        public DocumentUploadDto Dto { get; }

        public CreateDocumentWithFileCommand(DocumentUploadDto dto)
        {
            Dto = dto;
        }
    }
}
