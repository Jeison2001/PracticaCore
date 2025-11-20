using Application.Shared.DTOs.Document;
using MediatR;

namespace Application.Shared.Commands.Document
{
    public class CreateDocumentWithFileCommand : IRequest<DocumentDto>
    {
        public DocumentUploadDto Dto { get; }

        public CreateDocumentWithFileCommand(DocumentUploadDto dto)
        {
            Dto = dto;
        }
    }
}
