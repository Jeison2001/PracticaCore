using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands
{
    public class DeleteEntityCommandHandler<T, TId> : IRequestHandler<DeleteEntityCommand<T, TId>, bool>
        where T : BaseEntity<TId>
        where TId : struct
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IUnitOfWork _unitOfWork;
        public DeleteEntityCommandHandler(IRepository<T, TId> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteEntityCommand<T, TId> request, CancellationToken ct)
        {
            var existingEntity = await _repository.GetByIdAsync(request.Id) ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");
            await _repository.DeleteAsync(existingEntity);
            await _unitOfWork.CommitAsync(ct);
            return true;
        }
    }
}
