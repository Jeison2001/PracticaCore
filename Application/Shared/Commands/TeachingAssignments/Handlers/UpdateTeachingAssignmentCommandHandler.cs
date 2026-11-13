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
                x.Id != request.Id,
                cancellationToken);
            if (exists != null)
                return _mapper.Map<TeachingAssignmentDto>(entity);

            // Capturar solo el valor necesario para notificaciones ANTES de modificar
            var wasConfirmed = entity.IsConfirmed;
            var originalTeacherId = entity.IdTeacher;
            var originalTypeId = entity.IdTypeTeachingAssignment;

            // Preservar campos de auditoría inmutables
            var originalCreatedAt = entity.CreatedAt;
            var originalIdUserCreatedAt = entity.IdUserCreatedAt;

            // Mapear cambios
            _mapper.Map(dto, entity);

            // Restaurar campos inmutables y asegurar UTC
            entity.CreatedAt = originalCreatedAt;
            entity.IdUserCreatedAt = originalIdUserCreatedAt;
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.IsConfirmed = dto.StatusRegister ? true : wasConfirmed;

            var typeRepo = _unitOfWork.GetRepository<TypeTeachingAssignment, int>();
            var type = await typeRepo.GetByIdAsync(entity.IdTypeTeachingAssignment);

            var isActivatingFromUnconfirmed = !wasConfirmed && entity.StatusRegister && entity.RevocationDate == null;
            var isReassigningConfirmed = wasConfirmed &&
                (originalTeacherId != entity.IdTeacher || originalTypeId != entity.IdTypeTeachingAssignment) &&
                entity.StatusRegister &&
                entity.RevocationDate == null;

            if ((isActivatingFromUnconfirmed || isReassigningConfirmed) && type != null)
            {
                await TeachingAssignmentEvaluationHelper.ValidateMaxAssignmentsAsync(
                    _repository,
                    type,
                    entity.IdTeacher,
                    excludeAssignmentId: entity.Id,
                    cancellationToken);
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);

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
                    cancellationToken);
            }

            // Procesar notificaciones en background
            if (isActivatingFromUnconfirmed)
            {
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentCreationAsync(entity.Id));
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

