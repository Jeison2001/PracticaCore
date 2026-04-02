using Application.Shared.DTOs.Documents;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.Documents
{
    public record CreateDocumentWithFileCommand : IRequest<DocumentDto>
    {
        public DocumentUploadDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }

        public CreateDocumentWithFileCommand(DocumentUploadDto dto, CurrentUserInfo currentUser)
        {
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}
