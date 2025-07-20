using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
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
        private readonly IEntityNotificationService? _notificationService;
        private readonly ILogger<UpdateEntityCommandHandler<T, TId, TDto>> _logger;

        public UpdateEntityCommandHandler(
            IRepository<T, TId> repository, 
            IMapper mapper, 
            IUnitOfWork unitOfWork,
            ILogger<UpdateEntityCommandHandler<T, TId, TDto>> logger,
            IEntityNotificationService? notificationService = null)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<TDto> Handle(UpdateEntityCommand<T, TId, TDto> request, CancellationToken ct)
        {
            var existingEntity = await _repository.GetByIdAsync(request.Id) ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");
            
            // Log del estado inicial
            if (existingEntity is InscriptionModality inscriptionBefore)
            {
                _logger.LogInformation("📋 Estado ANTES - ID: {Id}, IdStateInscription: {StateId}", 
                    inscriptionBefore.Id, inscriptionBefore.IdStateInscription);
            }
            
            // Crear copia profunda del estado original ANTES de cualquier modificación
            // Usar serialización JSON para evitar referencias compartidas
            var originalEntityJson = JsonSerializer.Serialize(existingEntity);
            var originalEntity = JsonSerializer.Deserialize<T>(originalEntityJson)!;
            
            // Log de la copia original
            if (originalEntity is InscriptionModality originalInscription)
            {
                _logger.LogInformation("📄 Copia ORIGINAL - ID: {Id}, IdStateInscription: {StateId}", 
                    originalInscription.Id, originalInscription.IdStateInscription);
            }
            
            var originalId = existingEntity.Id;
            var originalCreatedAt = existingEntity.CreatedAt;
            var originalIdUserCreatedAt = existingEntity.IdUserCreatedAt;

            // Mapear el DTO a la entidad existente (DESPUÉS de crear la copia)
            _mapper.Map(request.Dto, existingEntity);
            
            // Log después del mapeo
            if (existingEntity is InscriptionModality inscriptionAfter)
            {
                _logger.LogInformation("📝 Estado DESPUÉS del mapeo - ID: {Id}, IdStateInscription: {StateId}", 
                    inscriptionAfter.Id, inscriptionAfter.IdStateInscription);
            }
            
            // Verificar si la copia original se mantiene intacta
            if (originalEntity is InscriptionModality originalCheck)
            {
                _logger.LogInformation("🔍 Verificando copia original - ID: {Id}, IdStateInscription: {StateId}", 
                    originalCheck.Id, originalCheck.IdStateInscription);
            }

            // Restaurar campos inmutables
            existingEntity.Id = originalId;
            existingEntity.CreatedAt = DateTime.SpecifyKind(originalCreatedAt, DateTimeKind.Utc);
            existingEntity.IdUserCreatedAt = originalIdUserCreatedAt;

            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);
            await _unitOfWork.CommitAsync(ct);

            // Procesar notificaciones automáticas si el servicio está disponible
            _logger.LogInformation("🔔 Verificando notificaciones - Servicio disponible: {ServiceAvailable}, Es InscriptionModality: {IsInscriptionModality}", 
                _notificationService != null, existingEntity is InscriptionModality);
                
            if (_notificationService != null && existingEntity is InscriptionModality)
            {
                _logger.LogInformation("📧 Procesando notificaciones para InscriptionModality ID: {Id}", existingEntity.Id);
                await _notificationService.ProcessInscriptionModalityChangesAsync(originalEntity, existingEntity, ct);
            }
            else if (_notificationService == null)
            {
                _logger.LogWarning("⚠️ IEntityNotificationService no está disponible - notificaciones no se procesarán");
            }

            return _mapper.Map<TDto>(existingEntity);
        }
    }
}
