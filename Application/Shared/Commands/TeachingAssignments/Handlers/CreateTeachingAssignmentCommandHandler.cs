using Application.Common.Services;
using Application.Common.Services.Jobs;
using Application.Shared.DTOs.TeachingAssignments;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.TeachingAssignments.Handlers
{
    /// <summary>
    /// Crea una asignación docente tras validar que el docente tiene habilitado el cargo
    /// (TeacherEnabledCargo) y el límite de MaxAssignments para la combinación docente/cargo.
    /// Maneja race condition: si ocurre una violación de constraint única (23505),
    /// retorna la asignación existente en lugar de fallar — previene notificaciones duplicadas
    /// cuando dos solicitudes compiten. Encola HandleTeachingAssignmentCreationAsync tras creación.
    /// </summary>
    public class CreateTeachingAssignmentCommandHandler : IRequestHandler<CreateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IRepository<TypeTeachingAssignment, int> _typeRepository;
        private readonly IRepository<TeacherEnabledCargo, int> _teacherEnabledCargoRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<CreateTeachingAssignmentCommandHandler> _logger;

        public CreateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IRepository<TypeTeachingAssignment, int> typeRepository,
            IRepository<TeacherEnabledCargo, int> teacherEnabledCargoRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer,
            ILogger<CreateTeachingAssignmentCommandHandler> logger)
        {
            _repository = repository;
            _typeRepository = typeRepository;
            _teacherEnabledCargoRepository = teacherEnabledCargoRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<TeachingAssignmentDto> Handle(CreateTeachingAssignmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            // Obtener el tipo de asignación para conocer el código y límite
            var type = await _typeRepository.GetByIdAsync(dto.IdTypeTeachingAssignment);
            if (type == null)
                throw new InvalidOperationException("Tipo de asignación docente no encontrado.");

            await TeachingAssignmentEvaluationHelper.ValidateCargoAllowedForModalityAsync(
                _unitOfWork,
                dto.IdInscriptionModality,
                type,
                cancellationToken);

            if (request.CurrentUser?.UserId is not int currentUserId ||
                currentUserId <= 0 ||
                request.CurrentUser.Permissions is not { Count: > 0 } perms)
            {
                throw new ForbiddenAccessException("No tiene permisos vigentes para crear asignaciones docentes.");
            }

            var hasOfficialPermission =
                perms.Contains(PermissionCodes.ProyectoGrado.N4PGPPA) ||
                perms.Contains(PermissionCodes.ProyectoGrado.N4PGPAA) ||
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF0PA);

            if (dto.IsConfirmed && !hasOfficialPermission)
            {
                throw new ForbiddenAccessException("No tiene permisos vigentes para crear asignaciones docentes confirmadas.");
            }

            if (!dto.IsConfirmed)
            {
                if (!hasOfficialPermission)
                {
                    var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                    var isActiveMember = await uimRepo.AnyAsync(
                        x => x.IdInscriptionModality == dto.IdInscriptionModality &&
                             x.IdUser == currentUserId &&
                             x.StatusRegister &&
                             x.IsConfirmed,
                        cancellationToken);

                    if (!isActiveMember)
                        throw new ForbiddenAccessException("Solo los estudiantes activos de la inscripción pueden sugerir docentes.");
                }

                if (string.Equals(type.Code, TeachingAssignmentEvaluationHelper.JuradoEvaluadorCode, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("No está permitido preseleccionar un Jurado Evaluador.");
            }

            // Validar que el docente tiene habilitado este cargo (TeacherEnabledCargo)
            var cargoHabilitado = await _teacherEnabledCargoRepository.AnyAsync(
                x => x.IdUser == dto.IdTeacher &&
                     x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                     x.StatusRegister,
                cancellationToken);
            if (!cargoHabilitado)
                throw new InvalidOperationException($"El docente no tiene habilitado el cargo '{type.Name}' para asignaciones docentes.");

            // Verificar si ya existe una asignación activa para el mismo teacher, inscripcion y cargo
            var existing = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdTeacher == dto.IdTeacher &&
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister,
                cancellationToken);
            if (existing != null)
            {
                if (!existing.IsConfirmed && dto.IsConfirmed)
                {
                    var promoted = await PromoteUnconfirmedAssignmentAsync(existing, type, dto, request.CurrentUser.UserId, cancellationToken);
                    if (promoted)
                    {
                        await NotifyAndTryAdvancePreliminaryProjectAsync(existing, type, request.CurrentUser.UserId, cancellationToken);
                    }
                }

                return _mapper.Map<TeachingAssignmentDto>(existing);
            }

            if (!dto.IsConfirmed)
            {
                return await CreateUnconfirmedSuggestionAsync(dto, request.CurrentUser.UserId, cancellationToken);
            }

            var uniqueRaceDetected = false;
            var createdOrPromoted = false;
            TeachingAssignment? resultEntity = null;

            await using (var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                await using var quotaLock = await _repository.AcquireTeacherCargoQuotaLockAsync(
                    dto.IdTeacher,
                    type.Id,
                    cancellationToken);

                var raceExisting = await _repository.GetFirstOrDefaultAsync(x =>
                    x.IdTeacher == dto.IdTeacher &&
                    x.IdInscriptionModality == dto.IdInscriptionModality &&
                    x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                    x.StatusRegister,
                    cancellationToken);

                if (raceExisting != null)
                {
                    resultEntity = raceExisting;
                    if (!raceExisting.IsConfirmed)
                    {
                        createdOrPromoted = await PromoteUnderActiveLockAsync(
                            raceExisting,
                            type,
                            dto,
                            request.CurrentUser.UserId,
                            transaction,
                            cancellationToken);
                    }
                    else
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                }
                else
                {
                    await TeachingAssignmentEvaluationHelper.ValidateMaxAssignmentsAsync(
                        _repository,
                        type,
                        dto.IdTeacher,
                        excludeAssignmentId: null,
                        cancellationToken);

                    var entity = _mapper.Map<TeachingAssignment>(dto);
                    entity.IsConfirmed = true;
                    entity.IdUserCreatedAt = request.CurrentUser.UserId;

                    try
                    {
                        await _repository.AddAsync(entity);
                        await _unitOfWork.CommitAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        resultEntity = entity;
                        createdOrPromoted = true;
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        await _repository.DeleteAsync(entity);
                        await transaction.RollbackAsync(cancellationToken);
                        uniqueRaceDetected = true;
                    }
                }
            }

            if (uniqueRaceDetected)
            {
                _logger.LogWarning(
                    "Condición de carrera prevenida: El docente {IdTeacher} ya fue asignado activamente en la inscripción {IdInscriptionModality} con el cargo {IdTypeTeachingAssignment}.",
                    dto.IdTeacher,
                    dto.IdInscriptionModality,
                    dto.IdTypeTeachingAssignment);

                var raceConditionExisting = await _repository.GetFirstOrDefaultAsync(x =>
                    x.IdTeacher == dto.IdTeacher &&
                    x.IdInscriptionModality == dto.IdInscriptionModality &&
                    x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                    x.StatusRegister,
                    cancellationToken);

                if (raceConditionExisting == null)
                    throw new InvalidOperationException("No se pudo recuperar la asignación docente tras un conflicto de concurrencia.");

                if (!raceConditionExisting.IsConfirmed)
                {
                    var promoted = await PromoteUnconfirmedAssignmentAsync(
                        raceConditionExisting,
                        type,
                        dto,
                        request.CurrentUser.UserId,
                        cancellationToken);

                    if (promoted)
                    {
                        await NotifyAndTryAdvancePreliminaryProjectAsync(
                            raceConditionExisting,
                            type,
                            request.CurrentUser.UserId,
                            cancellationToken);
                    }
                }

                return _mapper.Map<TeachingAssignmentDto>(raceConditionExisting);
            }

            if (createdOrPromoted && resultEntity != null)
            {
                await NotifyAndTryAdvancePreliminaryProjectAsync(
                    resultEntity,
                    type,
                    request.CurrentUser.UserId,
                    cancellationToken);
            }

            return _mapper.Map<TeachingAssignmentDto>(resultEntity!);
        }

        private async Task<TeachingAssignmentDto> CreateUnconfirmedSuggestionAsync(
            TeachingAssignmentDto dto,
            int? userId,
            CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<TeachingAssignment>(dto);
            entity.IsConfirmed = false;
            entity.IdUserCreatedAt = userId;

            try
            {
                await _repository.AddAsync(entity);
                await _unitOfWork.CommitAsync(cancellationToken);
                return _mapper.Map<TeachingAssignmentDto>(entity);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(
                    "Condición de carrera prevenida al sugerir asignación: El docente {IdTeacher} ya fue asignado activamente en la inscripción {IdInscriptionModality} con el cargo {IdTypeTeachingAssignment}.",
                    dto.IdTeacher,
                    dto.IdInscriptionModality,
                    dto.IdTypeTeachingAssignment);

                await _repository.DeleteAsync(entity);

                var raceConditionExisting = await _repository.GetFirstOrDefaultAsync(x =>
                    x.IdTeacher == dto.IdTeacher &&
                    x.IdInscriptionModality == dto.IdInscriptionModality &&
                    x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                    x.StatusRegister,
                    cancellationToken);

                if (raceConditionExisting != null)
                    return _mapper.Map<TeachingAssignmentDto>(raceConditionExisting);

                throw;
            }
        }

        private async Task<bool> PromoteUnconfirmedAssignmentAsync(
            TeachingAssignment existing,
            TypeTeachingAssignment type,
            TeachingAssignmentDto dto,
            int? userId,
            CancellationToken cancellationToken)
        {
            if (existing.IsConfirmed)
                return false;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await using var quotaLock = await _repository.AcquireTeacherCargoQuotaLockAsync(
                dto.IdTeacher,
                type.Id,
                cancellationToken);

            return await PromoteUnderActiveLockAsync(
                existing,
                type,
                dto,
                userId,
                transaction,
                cancellationToken);
        }

        private async Task<bool> PromoteUnderActiveLockAsync(
            TeachingAssignment existing,
            TypeTeachingAssignment type,
            TeachingAssignmentDto dto,
            int? userId,
            IDatabaseTransaction transaction,
            CancellationToken cancellationToken)
        {
            if (existing.IsConfirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await TeachingAssignmentEvaluationHelper.ValidateMaxAssignmentsAsync(
                _repository,
                type,
                dto.IdTeacher,
                excludeAssignmentId: existing.Id,
                cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var operationRegister = dto.OperationRegister ?? existing.OperationRegister;
            var confirmed = await _repository.TryConfirmAssignmentAtomicallyAsync(
                existing.Id,
                operationRegister,
                now,
                userId,
                cancellationToken);

            if (!confirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);

            existing.IsConfirmed = true;
            existing.OperationRegister = operationRegister;
            existing.UpdatedAt = now;
            existing.IdUserUpdatedAt = userId;

            return true;
        }

        private async Task NotifyAndTryAdvancePreliminaryProjectAsync(
            TeachingAssignment entity,
            TypeTeachingAssignment type,
            int? userId,
            CancellationToken cancellationToken)
        {
            if (!entity.IsConfirmed)
                return;

            ProcessNotificationsAsync(entity);

            if (string.Equals(type.Code, TeachingAssignmentEvaluationHelper.JuradoEvaluadorCode, StringComparison.OrdinalIgnoreCase) &&
                entity.StatusRegister &&
                entity.RevocationDate == null)
            {
                await TeachingAssignmentEvaluationHelper.TryAdvancePreliminaryProjectToEvaluationAsync(
                    _unitOfWork,
                    _repository,
                    entity.IdInscriptionModality,
                    type.Id,
                    userId,
                    cancellationToken,
                    _jobEnqueuer);
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var innerExceptionType = ex.InnerException?.GetType().Name;
            var sqlStateProperty = ex.InnerException?.GetType().GetProperty("SqlState");
            var sqlState = sqlStateProperty?.GetValue(ex.InnerException) as string;
            return innerExceptionType == "PostgresException" && sqlState == "23505";
        }

        private void ProcessNotificationsAsync(TeachingAssignment entity)
        {
            // Fire-and-forget seguro usando Hangfire mediante dispatcher genérico envoltorio
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentCreationAsync(entity.Id));
        }
    }
}
