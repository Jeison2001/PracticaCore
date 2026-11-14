using Application.Common.Services;
using Application.Common.Services.Jobs;
using Application.Shared.DTOs.TeachingAssignments;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.TeachingAssignments.Handlers
{
    /// <summary>
    /// Actualiza una asignación docente. Preserva campos de auditoría immutables (CreatedAt, IdUserCreatedAt).
    /// Encola HandleTeachingAssignmentChangeAsync que notifica tanto al nuevo docente como al anterior
    /// (en caso de que la asignación haya sido reasignada).
    /// </summary>
    public class UpdateTeachingAssignmentCommandHandler : IRequestHandler<UpdateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<UpdateTeachingAssignmentCommandHandler> _logger;

        public UpdateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer,
            ILogger<UpdateTeachingAssignmentCommandHandler> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<TeachingAssignmentDto> Handle(UpdateTeachingAssignmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new InvalidOperationException("Asignación docente no encontrada.");

            // Validar duplicidad
            var exists = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTeacher == dto.IdTeacher &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister &&
                x.RevocationDate == null &&
                x.Id != request.Id,
                cancellationToken);
            if (exists != null)
                throw new InvalidOperationException("Ya existe una asignación docente activa para este docente, cargo e inscripción.");

            // Capturar valores originales antes de modificar para auditoría y notificaciones
            var wasConfirmed = entity.IsConfirmed;
            var wasActive = entity.StatusRegister && entity.RevocationDate == null;
            var originalTeacherId = entity.IdTeacher;
            var originalTypeId = entity.IdTypeTeachingAssignment;

            // Preservar campos de auditoría inmutables
            var originalCreatedAt = entity.CreatedAt;
            var originalIdUserCreatedAt = entity.IdUserCreatedAt;

            var typeRepo = _unitOfWork.GetRepository<TypeTeachingAssignment, int>();
            var type = await typeRepo.GetByIdAsync(dto.IdTypeTeachingAssignment);

            var targetActive = dto.StatusRegister && dto.RevocationDate == null;
            if (targetActive)
            {
                await TeachingAssignmentEvaluationHelper.ValidateCargoAllowedForModalityAsync(
                    _unitOfWork,
                    dto.IdInscriptionModality,
                    type,
                    cancellationToken);
            }

            if (targetActive && (!wasActive || originalTeacherId != dto.IdTeacher || originalTypeId != dto.IdTypeTeachingAssignment))
            {
                var enabledCargoRepo = _unitOfWork.GetRepository<TeacherEnabledCargo, int>();
                var cargoHabilitado = await enabledCargoRepo.AnyAsync(
                    x => x.IdUser == dto.IdTeacher &&
                         x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                         x.StatusRegister,
                    cancellationToken);

                if (!cargoHabilitado)
                {
                    var typeName = type?.Name ?? dto.IdTypeTeachingAssignment.ToString();
                    throw new InvalidOperationException($"El docente no tiene habilitado el cargo '{typeName}' para asignaciones docentes.");
                }
            }

            var isActivatingFromUnconfirmed = !wasConfirmed && targetActive;
            var isReactivatingConfirmed = !wasActive && wasConfirmed && targetActive;
            var isReassigningConfirmed = wasConfirmed &&
                (originalTeacherId != dto.IdTeacher || originalTypeId != dto.IdTypeTeachingAssignment) &&
                targetActive;

            var requiresMaxAssignmentsCheck = (isActivatingFromUnconfirmed || isReactivatingConfirmed || isReassigningConfirmed) && type != null;
            var needsQuotaLock = type != null && (requiresMaxAssignmentsCheck || (wasConfirmed && targetActive));
            var confirmedByThisUpdate = false;

            await using (var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                await using var quotaLock = needsQuotaLock
                    ? await _repository.AcquireTeacherCargoQuotaLockAsync(dto.IdTeacher, type!.Id, cancellationToken)
                    : null;

                if (requiresMaxAssignmentsCheck)
                {
                    await TeachingAssignmentEvaluationHelper.ValidateMaxAssignmentsAsync(
                        _repository,
                        type!,
                        dto.IdTeacher,
                        excludeAssignmentId: entity.Id,
                        cancellationToken);
                }

                if (isActivatingFromUnconfirmed)
                {
                    confirmedByThisUpdate = await _repository.TryConfirmAssignmentAtomicallyAsync(
                        entity.Id,
                        dto.OperationRegister,
                        DateTimeOffset.UtcNow,
                        request.CurrentUser.UserId,
                        cancellationToken);
                }

                var effectiveWasConfirmed = wasConfirmed || (isActivatingFromUnconfirmed && entity.IsConfirmed);

                // Mapear cambios
                _mapper.Map(dto, entity);

                // Mantener coherencia estricta entre RevocationDate y StatusRegister
                if (entity.RevocationDate != null)
                {
                    entity.StatusRegister = false;
                }
                else if (!entity.StatusRegister)
                {
                    entity.RevocationDate = DateTimeOffset.UtcNow;
                }

                // Restaurar campos inmutables y asegurar UTC
                entity.CreatedAt = originalCreatedAt;
                entity.IdUserCreatedAt = originalIdUserCreatedAt;
                entity.IdUserUpdatedAt = request.CurrentUser.UserId;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                entity.IsConfirmed = (entity.StatusRegister && entity.RevocationDate == null) ? true : effectiveWasConfirmed;

                await _repository.UpdateAsync(entity);
                await _unitOfWork.CommitAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            if (entity.IsConfirmed &&
                entity.StatusRegister &&
                entity.RevocationDate == null &&
                type != null &&
                string.Equals(type.Code, TeachingAssignmentEvaluationHelper.JuradoEvaluadorCode, StringComparison.OrdinalIgnoreCase))
            {
                await TeachingAssignmentEvaluationHelper.TryAdvancePreliminaryProjectToEvaluationAsync(
                    _unitOfWork,
                    _repository,
                    entity.IdInscriptionModality,
                    type.Id,
                    request.CurrentUser.UserId,
                    cancellationToken,
                    _jobEnqueuer);
            }

            // Procesar notificaciones en background (solo quien confirmó efectivamente emite creación)
            if (isActivatingFromUnconfirmed)
            {
                if (confirmedByThisUpdate)
                {
                    _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentCreationAsync(entity.Id));
                }
                else if (originalTeacherId != entity.IdTeacher)
                {
                    ProcessNotificationsAsync(entity, originalTeacherId);
                }
            }
            else if (wasConfirmed)
            {
                ProcessNotificationsAsync(entity, originalTeacherId);
            }

            return _mapper.Map<TeachingAssignmentDto>(entity);
        }

        private void ProcessNotificationsAsync(TeachingAssignment updatedEntity, int originalTeacherId)
        {
            // Fire-and-forget seguro usando Hangfire
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentChangeAsync(updatedEntity.Id, originalTeacherId));
        }
    }
}

