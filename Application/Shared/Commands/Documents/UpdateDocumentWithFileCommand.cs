using Application.Shared.DTOs.Documents;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.Documents
{
    public record UpdateDocumentWithFileCommand : IRequest<DocumentDto>
    {
        public int Id { get; }
        public DocumentUpdateDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }

        public UpdateDocumentWithFileCommand(int id, DocumentUpdateDto dto, CurrentUserInfo currentUser)
        {
            Id = id;
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}
