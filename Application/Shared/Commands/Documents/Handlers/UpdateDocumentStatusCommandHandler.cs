using MediatR;
using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Commands.Documents.Handlers
{
    /// <summary>
    /// Soft-delete / actualización de solo estado para Document. Actualiza StatusRegister,
    /// OperationRegister, UpdatedAt e IdUserUpdatedAt. Usado por el endpoint UpdateStatus
    /// de DocumentController.
    /// </summary>
    public class UpdateDocumentStatusCommandHandler : IRequestHandler<UpdateDocumentStatusCommand, bool>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateDocumentStatusCommandHandler(IRepository<Document, int> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateDocumentStatusCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                return false;

            entity.StatusRegister = request.StatusRegister;
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.OperationRegister = request.OperationRegister ?? string.Empty;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _repository.UpdatePartialAsync(entity, new Expression<Func<Document, object?>>[] {
                e => e.StatusRegister,
                e => e.IdUserUpdatedAt,
                e => e.OperationRegister,
                e => e.UpdatedAt
            });

            await _unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
    }
}
