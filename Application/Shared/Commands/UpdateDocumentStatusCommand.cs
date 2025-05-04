using MediatR;

namespace Application.Shared.Commands
{
    public class UpdateDocumentStatusCommand : IRequest<bool>
    {
        public int Id { get; }
        public bool StatusRegister { get; }
        public int? IdUserUpdateAt { get; }
        public string? OperationRegister { get; }

        public UpdateDocumentStatusCommand(int id, bool statusRegister, int? idUserUpdateAt, string? operationRegister)
        {
            Id = id;
            StatusRegister = statusRegister;
            IdUserUpdateAt = idUserUpdateAt;
            OperationRegister = operationRegister;
        }
    }
}
