using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands
{
    public class UpdateStatusEntityCommandHandler<T, TId> : IRequestHandler<UpdateStatusEntityCommand<T, TId>, bool>
        where T : BaseEntity<TId>
        where TId : struct
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateStatusEntityCommandHandler(IRepository<T, TId> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateStatusEntityCommand<T, TId> request, CancellationToken ct)
        {
            var existingEntity = await _repository.GetByIdAsync(request.Id) ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");

            // Actualizar los campos para el borrado lógico
            existingEntity.StatusRegister = request.StatusRegister;
            existingEntity.UpdatedAt = DateTime.UtcNow;
            existingEntity.IdUserUpdatedAt = request.IdUserUpdateAt;
            existingEntity.OperationRegister = request.OperationRegister;

            await _repository.UpdatePartialAsync(existingEntity, new Expression<Func<T, object>>[] {
                e => e.StatusRegister,
                e => e.UpdatedAt,
                e => e.IdUserUpdatedAt,
                e => e.OperationRegister
            });
            await _unitOfWork.CommitAsync(ct);
            return true;
        }
    }
}
