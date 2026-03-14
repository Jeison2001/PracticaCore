using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Dispatcher;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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
            
            // Crear copia profunda del estado original ANTES de cualquier modificaci�n
            // Usar serializaci�n JSON para evitar referencias compartidas
            var originalEntityJson = JsonSerializer.Serialize(existingEntity);
            var originalEntity = JsonSerializer.Deserialize<T>(originalEntityJson)!;
            
            var originalId = existingEntity.Id;
            var originalCreatedAt = existingEntity.CreatedAt;
            var originalIdUserCreatedAt = existingEntity.IdUserCreatedAt;

            // Mapear el DTO a la entidad existente (DESPU�S de crear la copia)
            _mapper.Map(request.Dto, existingEntity);

            // Restaurar campos inmutables
            existingEntity.Id = originalId;
            existingEntity.CreatedAt = DateTime.SpecifyKind(originalCreatedAt, DateTimeKind.Utc);
            existingEntity.IdUserCreatedAt = originalIdUserCreatedAt;

            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);
            await _unitOfWork.CommitAsync(ct);

            // Procesar notificaciones en background
            ProcessNotificationsAsync(existingEntity, originalEntity);

            return _mapper.Map<TDto>(existingEntity);
        }

        private void ProcessNotificationsAsync(T updatedEntity, T originalEntity)
        {
            if (_jobEnqueuer != null)
            {
                if (typeof(T) == typeof(Proposal))
                {
                     var proposal = updatedEntity as Proposal;
                     var originalProposal = originalEntity as Proposal;
                     if (proposal != null && originalProposal != null)
                     {
                         _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleProposalChangeAsync(proposal.Id, originalProposal.IdStateStage));
                     }
                }
            }
        }
    }
}