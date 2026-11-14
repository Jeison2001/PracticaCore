using Application.Common.Services;
using Application.Common.Services.Jobs;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.TeachingAssignments.Handlers
{
    public class UpdateTeachingAssignmentStatusCommandHandler : IRequestHandler<UpdateTeachingAssignmentStatusCommand, bool>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<UpdateTeachingAssignmentStatusCommandHandler> _logger;

        public UpdateTeachingAssignmentStatusCommandHandler(
            ITeachingAssignmentRepository repository,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer,
            ILogger<UpdateTeachingAssignmentStatusCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateTeachingAssignmentStatusCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"TeachingAssignment with ID {request.Id} not found.");

            var wasConfirmed = entity.IsConfirmed;
            var wasActive = entity.StatusRegister && entity.RevocationDate == null;
            var type = await _unitOfWork.GetRepository<TypeTeachingAssignment, int>()
                .GetByIdAsync(entity.IdTypeTeachingAssignment);

            var confirmedByThisUpdate = false;

            if (request.StatusRegister)
            {
                var duplicate = await _repository.GetFirstOrDefaultAsync(x =>
                    x.IdInscriptionModality == entity.IdInscriptionModality &&
                    x.IdTeacher == entity.IdTeacher &&
                    x.IdTypeTeachingAssignment == entity.IdTypeTeachingAssignment &&
                    x.StatusRegister &&
                    x.RevocationDate == null &&
                    x.Id != entity.Id,
                    cancellationToken);

                if (duplicate != null)
                {
                    throw new InvalidOperationException(
                        "Ya existe una asignación docente activa para este docente, cargo e inscripción.");
                }

                await TeachingAssignmentEvaluationHelper.ValidateCargoAllowedForModalityAsync(
                    _unitOfWork,
                    entity.IdInscriptionModality,
                    type,
                    cancellationToken);

                if (!wasActive)
                {
                    var cargoHabilitado = await _unitOfWork.GetRepository<TeacherEnabledCargo, int>().AnyAsync(
                        x => x.IdUser == entity.IdTeacher &&
                             x.IdTypeTeachingAssignment == entity.IdTypeTeachingAssignment &&
                             x.StatusRegister,
                        cancellationToken);

                    if (!cargoHabilitado)
                    {
                        var typeName = type?.Name ?? entity.IdTypeTeachingAssignment.ToString();
                        throw new InvalidOperationException(
                            $"El docente no tiene habilitado el cargo '{typeName}' para asignaciones docentes.");
                    }
                }

                var needsQuotaCheck = (!wasConfirmed || !wasActive) && type != null;

                await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await using var quotaLock = type != null
                    ? await _repository.AcquireTeacherCargoQuotaLockAsync(entity.IdTeacher, type.Id, cancellationToken)
                    : null;

                if (needsQuotaCheck)
                {
                    await TeachingAssignmentEvaluationHelper.ValidateMaxAssignmentsAsync(
                        _repository,
                        type!,
                        entity.IdTeacher,
                        excludeAssignmentId: entity.Id,
                        cancellationToken);
                }

                if (!wasConfirmed)
                {
                    confirmedByThisUpdate = wasActive
                        ? await _repository.TryConfirmAssignmentAtomicallyAsync(
                            entity.Id,
                            request.OperationRegister,
                            DateTimeOffset.UtcNow,
                            request.CurrentUser.UserId,
                            cancellationToken)
                        : true;
                }

                entity.StatusRegister = true;
                entity.RevocationDate = null;
                entity.IsConfirmed = true;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                entity.IdUserUpdatedAt = request.CurrentUser.UserId;
                entity.OperationRegister = request.OperationRegister ?? entity.OperationRegister ?? "STATUS_UPDATE";

                await _repository.UpdatePartialAsync(entity, new Expression<Func<TeachingAssignment, object?>>[]
                {
                    e => e.StatusRegister,
                    e => e.RevocationDate,
                    e => e.IsConfirmed,
                    e => e.UpdatedAt,
                    e => e.IdUserUpdatedAt,
                    e => e.OperationRegister
                });
                await _unitOfWork.CommitAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                entity.StatusRegister = false;
                entity.IsConfirmed = false;
                entity.RevocationDate ??= DateTimeOffset.UtcNow;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                entity.IdUserUpdatedAt = request.CurrentUser.UserId;
                entity.OperationRegister = request.OperationRegister ?? entity.OperationRegister ?? "STATUS_UPDATE";

                await _repository.UpdatePartialAsync(entity, new Expression<Func<TeachingAssignment, object?>>[]
                {
                    e => e.StatusRegister,
                    e => e.IsConfirmed,
                    e => e.RevocationDate,
                    e => e.UpdatedAt,
                    e => e.IdUserUpdatedAt,
                    e => e.OperationRegister
                });
                await _unitOfWork.CommitAsync(cancellationToken);
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

            if (request.StatusRegister && !wasConfirmed)
            {
                if (confirmedByThisUpdate)
                {
                    _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentCreationAsync(entity.Id));
                }
            }
            else if (wasConfirmed && wasActive != request.StatusRegister)
            {
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentChangeAsync(entity.Id, entity.IdTeacher));
            }

            _logger.LogInformation(
                "TeachingAssignment Id={Id} status updated to {Status}.",
                entity.Id, entity.StatusRegister);

            return true;
        }
    }
}
