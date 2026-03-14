using Application.Shared.DTOs.Documents;
using MediatR;

namespace Application.Shared.Commands.Documents
{
    public record UpdateDocumentWithFileCommand : IRequest<DocumentDto>
    {
        public int Id { get; }
        public DocumentUpdateDto Dto { get; }

        public UpdateDocumentWithFileCommand(int id, DocumentUpdateDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }
}
