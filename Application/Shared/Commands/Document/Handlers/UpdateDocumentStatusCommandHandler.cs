using Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Application.Shared.Commands.Document.Handlers
{
    public class UpdateDocumentStatusCommandHandler : IRequestHandler<UpdateDocumentStatusCommand, bool>
    {
        private readonly IRepository<Domain.Entities.Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateDocumentStatusCommandHandler(IRepository<Domain.Entities.Document, int> repository, IUnitOfWork unitOfWork)
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
            entity.IdUserUpdatedAt = request.IdUserUpdateAt;
            entity.OperationRegister = request.OperationRegister ?? string.Empty;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdatePartialAsync(entity, new Expression<Func<Domain.Entities.Document, object?>>[] {
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
