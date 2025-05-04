using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Commands
{
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
            entity.IdUserUpdatedAt = request.IdUserUpdateAt;
            entity.OperationRegister = request.OperationRegister;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
    }
}
