using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.Documents
{
    public record UpdateDocumentStatusCommand : IRequest<bool>
    {
        public int Id { get; }
        public bool StatusRegister { get; }
        public CurrentUserInfo CurrentUser { get; }
        public string? OperationRegister { get; }

        public UpdateDocumentStatusCommand(int id, bool statusRegister, CurrentUserInfo currentUser, string? operationRegister)
        {
            Id = id;
            StatusRegister = statusRegister;
            CurrentUser = currentUser;
            OperationRegister = operationRegister;
        }
    }
}
