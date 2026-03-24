using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using Application.Common.Services.Jobs;

namespace Application.Shared.Commands.Handlers
{
    public class UpdateEntityCommandHandler<T, TId, TDto> : IRequestHandler<UpdateEntityCommand<T, TId, TDto>, TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer? _jobEnqueuer;
        private readonly ILogger<UpdateEntityCommandHandler<T, TId, TDto>> _logger;

        public UpdateEntityCommandHandler(
            IRepository<T, TId> repository, 
            IMapper mapper, 
            IUnitOfWork unitOfWork,
            ILogger<UpdateEntityCommandHandler<T, TId, TDto>> logger,
            IJobEnqueuer? jobEnqueuer = null)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<TDto> Handle(UpdateEntityCommand<T, TId, TDto> request, CancellationToken ct)
        {
            var existingEntity = await _repository.GetByIdAsync(request.Id) ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");

            // Capturar solo los valores necesarios para notificaciones ANTES de modificar
            int? originalStateId = CaptureStateIdIfNeeded(existingEntity);

            var originalId = existingEntity.Id;
            var originalCreatedAt = existingEntity.CreatedAt;
            var originalIdUserCreatedAt = existingEntity.IdUserCreatedAt;

            // Mapear el DTO a la entidad existente (despues de capturar valores originales)
            _mapper.Map(request.Dto, existingEntity);

            // Restaurar campos inmutables
            existingEntity.Id = originalId;
            existingEntity.CreatedAt = DateTime.SpecifyKind(originalCreatedAt, DateTimeKind.Utc);
            existingEntity.IdUserCreatedAt = originalIdUserCreatedAt;

            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);
            await _unitOfWork.CommitAsync(ct);

            // Procesar notificaciones en background
            ProcessNotificationsAsync(existingEntity, originalStateId);

            return _mapper.Map<TDto>(existingEntity);
        }

        private int? CaptureStateIdIfNeeded(T entity)
        {
            // Solo Proposal tiene notificación por cambio de estado en este handler genérico
            if (typeof(T) == typeof(Proposal))
            {
                return (entity as Proposal)?.IdStateStage;
            }
            return null;
        }

        private void ProcessNotificationsAsync(T updatedEntity, int? originalStateId)
        {
            if (_jobEnqueuer != null && originalStateId.HasValue)
            {
                if (typeof(T) == typeof(Proposal))
                {
                    var proposal = updatedEntity as Proposal;
                    if (proposal != null)
                    {
                        _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleProposalChangeAsync(proposal.Id, originalStateId.Value));
                    }
                }
            }
        }
    }
}