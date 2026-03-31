using Application.Shared.DTOs;
using Application.Common.Services.Jobs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

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
            var existingEntity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");

            // Capturar estado original para notificación
            int? originalStateId = CaptureStateIdIfNeeded(existingEntity);

            // Capturar valores originales de campos inmutables antes del mapeo
            var originalId = existingEntity.Id;
            var originalCreatedAt = existingEntity.CreatedAt;
            var originalIdUserCreatedAt = existingEntity.IdUserCreatedAt;

            // Mapear DTO a entidad
            _mapper.Map(request.Dto, existingEntity);

            // Restaurar campos inmutables - el frontend envía fechas en hora Colombia (Kind=Unspecified)
            existingEntity.Id = originalId;
            // Preservar la fecha de creación inmutable original
            existingEntity.IdUserCreatedAt = originalIdUserCreatedAt;
            existingEntity.CreatedAt = originalCreatedAt;

            // UpdatedAt se genera globalmente
            existingEntity.UpdatedAt = DateTimeOffset.UtcNow;

            // Nullificar navegaciones para evitar validaciones cruzadas EF Core
            NullifyNavigations(existingEntity);

            await _repository.UpdateAsync(existingEntity);
            await _unitOfWork.CommitAsync(ct);

            // Procesar notificaciones si cambió el estado
            ProcessNotificationsIfNeeded(existingEntity, originalStateId);

            return _mapper.Map<TDto>(existingEntity);
        }

        private void NullifyNavigations(T entity)
        {
            // El repository es BaseRepository en runtime, podemos acceder al Context vía reflection
            var repositoryType = _repository.GetType();
            var contextField = repositoryType.GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var context = contextField?.GetValue(_repository) as DbContext;
            if (context != null)
            {
                var entry = context.Entry(entity);
                foreach (var nav in entry.Navigations)
                {
                    nav.CurrentValue = null;
                }
            }
        }

        private int? CaptureStateIdIfNeeded(T entity)
        {
            if (typeof(T) == typeof(Proposal))
                return (entity as Proposal)?.IdStateStage;
            if (typeof(T) == typeof(PreliminaryProject))
                return (entity as PreliminaryProject)?.IdStateStage;
            if (typeof(T) == typeof(ProjectFinal))
                return (entity as ProjectFinal)?.IdStateStage;
            return null;
        }

        private void ProcessNotificationsIfNeeded(T entity, int? originalStateId)
        {
            if (_jobEnqueuer == null || !originalStateId.HasValue)
                return;

            if (typeof(T) == typeof(Proposal))
            {
                var proposal = entity as Proposal;
                if (proposal != null && proposal.IdStateStage != originalStateId.Value)
                {
                    _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                        x => x.HandleProposalChangeAsync(proposal.Id, originalStateId.Value));
                }
            }
            else if (typeof(T) == typeof(PreliminaryProject))
            {
                var preliminary = entity as PreliminaryProject;
                if (preliminary != null && preliminary.IdStateStage != originalStateId.Value)
                {
                    _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                        x => x.HandlePreliminaryProjectChangeAsync(preliminary.Id, originalStateId.Value));
                }
            }
            else if (typeof(T) == typeof(ProjectFinal))
            {
                var projectFinal = entity as ProjectFinal;
                if (projectFinal != null && projectFinal.IdStateStage != originalStateId.Value)
                {
                    _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                        x => x.HandleProjectFinalChangeAsync(projectFinal.Id, originalStateId.Value));
                }
            }
        }
    }
}

