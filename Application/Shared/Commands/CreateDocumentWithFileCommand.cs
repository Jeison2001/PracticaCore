using Application.Shared.DTOs.Document;
using MediatR;

namespace Application.Shared.Commands
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
