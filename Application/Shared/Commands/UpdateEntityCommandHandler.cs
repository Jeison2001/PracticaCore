using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
        private readonly INotificationDispatcher? _notificationDispatcher;
        private readonly ILogger<UpdateEntityCommandHandler<T, TId, TDto>> _logger;

        public UpdateEntityCommandHandler(
            IRepository<T, TId> repository, 
            IMapper mapper, 
            IUnitOfWork unitOfWork,
            ILogger<UpdateEntityCommandHandler<T, TId, TDto>> logger,
            INotificationDispatcher? notificationDispatcher = null)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _notificationDispatcher = notificationDispatcher;
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
            if (_notificationDispatcher != null)
            {
                // ✅ MEJORADO: Fire-and-forget seguro con manejo de scope
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogDebug("Dispatching update notification for {EntityType} ID: {Id}", typeof(T).Name, existingEntity.Id);
                        
                        // El dispatcher ahora maneja su propio scope internamente
                        await _notificationDispatcher.DispatchEntityChangeAsync<T, TId>(originalEntity, existingEntity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error dispatching update notification for {EntityType} ID: {Id}", typeof(T).Name, existingEntity.Id);
                    }
                });
            }
        }
    }
}
