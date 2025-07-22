using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ProposalNotificationService = Domain.Interfaces.Notifications.IProposalNotificationService;

namespace Application.Shared.Commands
{
    public class UpdateEntityCommandHandler<T, TId, TDto> : IRequestHandler<UpdateEntityCommand<T, TId, TDto>, TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEntityNotificationService? _notificationService;
        private readonly ProposalNotificationService? _proposalNotificationService;
        private readonly ILogger<UpdateEntityCommandHandler<T, TId, TDto>> _logger;

        public UpdateEntityCommandHandler(
            IRepository<T, TId> repository, 
            IMapper mapper, 
            IUnitOfWork unitOfWork,
            ILogger<UpdateEntityCommandHandler<T, TId, TDto>> logger,
            IEntityNotificationService? notificationService = null,
            ProposalNotificationService? proposalNotificationService = null)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _proposalNotificationService = proposalNotificationService;
            _logger = logger;
        }

        public async Task<TDto> Handle(UpdateEntityCommand<T, TId, TDto> request, CancellationToken ct)
        {
            var existingEntity = await _repository.GetByIdAsync(request.Id) ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");
            
            // Crear copia profunda del estado original ANTES de cualquier modificación
            // Usar serialización JSON para evitar referencias compartidas
            var originalEntityJson = JsonSerializer.Serialize(existingEntity);
            var originalEntity = JsonSerializer.Deserialize<T>(originalEntityJson)!;
            
            var originalId = existingEntity.Id;
            var originalCreatedAt = existingEntity.CreatedAt;
            var originalIdUserCreatedAt = existingEntity.IdUserCreatedAt;

            // Mapear el DTO a la entidad existente (DESPUÉS de crear la copia)
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

        private void ProcessNotificationsAsync(T existingEntity, T originalEntity)
        {
            // Notificación para Proposal en background
            if (_proposalNotificationService != null && typeof(T) == typeof(Proposal))
            {
                var proposalEntity = existingEntity as Proposal;
                if (proposalEntity != null)
                {
                    _logger.LogInformation("📧 Encolando notificación para Proposal ID: {Id}", proposalEntity.Id);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _proposalNotificationService.ProcessProposalEventAsync(proposalEntity, (Domain.Enums.StateStageEnum)proposalEntity.IdStateStage);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error procesando notificación para Proposal ID: {Id}", proposalEntity.Id);
                        }
                    });
                }
            }

            // Notificación para InscriptionModality en background
            if (_notificationService != null && typeof(T) == typeof(InscriptionModality))
            {
                var inscriptionEntity = existingEntity as InscriptionModality;
                if (inscriptionEntity != null)
                {
                    _logger.LogInformation("📧 Encolando notificación para InscriptionModality ID: {Id}", inscriptionEntity.Id);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.ProcessInscriptionModalityChangesAsync(originalEntity, existingEntity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error procesando notificación para InscriptionModality ID: {Id}", inscriptionEntity.Id);
                        }
                    });
                }
            }
        }
    }
}
