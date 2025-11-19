using Application.Shared.DTOs.Document;
using MediatR;

namespace Application.Shared.Commands
{
    public class UpdateDocumentWithFileCommand : IRequest<DocumentDto>
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
