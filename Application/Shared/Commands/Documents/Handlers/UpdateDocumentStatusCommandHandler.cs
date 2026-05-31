using MediatR;
using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Documents;

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
        private readonly IDocumentAccessGuard _accessGuard;

        public UpdateDocumentStatusCommandHandler(
            IRepository<Document, int> repository,
            IUnitOfWork unitOfWork,
            IDocumentAccessGuard accessGuard)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _accessGuard = accessGuard;
        }

        public async Task<bool> Handle(UpdateDocumentStatusCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                return false;

            // Seguridad (anti-IDOR): solo usuarios vinculados a la inscripcion pueden modificar
            // el documento. Logica centralizada y fail-closed en IDocumentAccessGuard. Ante fallo
            // lanza ForbiddenAccessException (403), NO devuelve false (que se confundiria con NotFound).
            await _accessGuard.EnsureUserCanModifyAsync(entity, request.CurrentUser.UserId ?? 0, cancellationToken);

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
