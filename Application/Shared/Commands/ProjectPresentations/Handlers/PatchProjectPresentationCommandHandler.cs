using Application.Common.Services;
using Application.Common.Services.Jobs;
using Application.Shared.DTOs.ProjectPresentations;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.ProjectPresentations.Handlers
{
    public class PatchProjectPresentationCommandHandler : IRequestHandler<PatchProjectPresentationCommand, ProjectPresentationDto>
    {
        private readonly IRepository<ProjectPresentation, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<PatchProjectPresentationCommandHandler> _logger;

        public PatchProjectPresentationCommandHandler(
            IRepository<ProjectPresentation, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJobEnqueuer jobEnqueuer,
            ILogger<PatchProjectPresentationCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<ProjectPresentationDto> Handle(PatchProjectPresentationCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"ProjectPresentation with ID {request.Id} not found.");

            var originalStateId = entity.IdStateStage;
            var originalObservations = entity.Observations;

            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var currentState = await stateStageRepo.GetByIdAsync(originalStateId);

            if (currentState != null && (currentState.IsFinalStateForModalityOverall || currentState.IsRejectionState))
            {
                throw new InvalidOperationException($"No se puede modificar el estado de un registro que ya se encuentra en estado '{currentState.Name}'.");
            }

            _logger.LogInformation(
                "Patching ProjectPresentation Id={Id}. Changes: StateStage={State}, ScheduledDateTime={SDate}, ActualDateTime={ADate}, LocationOrLink={Loc}, Modality={Mod}, Obs={Obs}",
                request.Id,
                request.Dto.IdStateStage,
                request.Dto.ScheduledDateTime,
                request.Dto.ActualDateTime,
                request.Dto.LocationOrLink,
                request.Dto.PresentationModality,
                request.Dto.Observations);

            var updatedProperties = new List<Expression<Func<ProjectPresentation, object?>>>();
            var actualDateTimeUpdated = false;

            // Siempre actualizar campos de tracking
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            updatedProperties.Add(x => x.IdUserUpdatedAt);
            updatedProperties.Add(x => x.UpdatedAt);

            // Actualizar campos escalares si vienen en el DTO
            if (request.Dto.ScheduledDateTime.HasValue)
            {
                entity.ScheduledDateTime = request.Dto.ScheduledDateTime.Value;
                updatedProperties.Add(x => x.ScheduledDateTime);
            }

            if (request.Dto.ActualDateTime.HasValue)
            {
                entity.ActualDateTime = request.Dto.ActualDateTime.Value;
                updatedProperties.Add(x => x.ActualDateTime);
                actualDateTimeUpdated = true;
            }

            if (request.Dto.LocationOrLink != null)
            {
                entity.LocationOrLink = request.Dto.LocationOrLink;
                updatedProperties.Add(x => x.LocationOrLink);
            }

            if (request.Dto.PresentationModality != null)
            {
                entity.PresentationModality = request.Dto.PresentationModality;
                updatedProperties.Add(x => x.PresentationModality);
            }

            var hasNewObservation = !string.IsNullOrWhiteSpace(request.Dto.Observations)
                && !string.Equals(request.Dto.Observations.Trim(), originalObservations?.Trim(), StringComparison.Ordinal);

            if (hasNewObservation)
            {
                entity.Observations = request.Dto.Observations!.Trim();
                updatedProperties.Add(x => x.Observations);
            }

            // Transición de estado
            if (request.Dto.IdStateStage.HasValue)
            {
                await ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                    _unitOfWork,
                    entity.Id,
                    originalStateId,
                    request.Dto.IdStateStage.Value,
                    cancellationToken);

                var newState = await stateStageRepo.GetByIdAsync(request.Dto.IdStateStage.Value);

                if (newState?.Code == StateStageCodes.SustProgramada)
                {
                    var scheduledDate = entity.ScheduledDateTime;
                    var modality = entity.PresentationModality;
                    var location = entity.LocationOrLink;

                    if (scheduledDate == null || string.IsNullOrWhiteSpace(modality) || string.IsNullOrWhiteSpace(location))
                    {
                        throw new InvalidOperationException("Para programar la sustentación debe especificar fecha/hora, modalidad y lugar o enlace.");
                    }

                    if (currentState?.Code == StateStageCodes.SustNoAprobadaReintento)
                    {
                        entity.AttemptNumber += 1;
                        updatedProperties.Add(x => x.AttemptNumber);
                    }
                }
                else if (newState?.Code == StateStageCodes.SustAprobada)
                {
                    entity.ActualDateTime = request.Dto.ActualDateTime ?? DateTimeOffset.UtcNow;
                    if (!actualDateTimeUpdated)
                    {
                        updatedProperties.Add(x => x.ActualDateTime);
                        actualDateTimeUpdated = true;
                    }

                    // Estampar FinalPhaseApprovalDate en ProjectFinal si existe y es null
                    var projectFinalRepo = _unitOfWork.GetRepository<ProjectFinal, int>();
                    var projectFinal = await projectFinalRepo.GetByIdAsync(entity.Id);
                    if (projectFinal != null && projectFinal.FinalPhaseApprovalDate == null)
                    {
                        projectFinal.FinalPhaseApprovalDate = entity.ActualDateTime;
                        projectFinal.UpdatedAt = DateTimeOffset.UtcNow;
                        await projectFinalRepo.UpdatePartialAsync(projectFinal, [x => x.FinalPhaseApprovalDate, x => x.UpdatedAt]);
                    }
                }
                else if (newState?.Code == StateStageCodes.SustNoAprobadaReintento)
                {
                    if (entity.AttemptNumber >= 2)
                    {
                        throw new InvalidOperationException("Según el Artículo 17 del Acuerdo 015, solo se permite una única oportunidad adicional de sustentación.");
                    }

                    entity.ActualDateTime = request.Dto.ActualDateTime ?? DateTimeOffset.UtcNow;
                    if (!actualDateTimeUpdated)
                    {
                        updatedProperties.Add(x => x.ActualDateTime);
                        actualDateTimeUpdated = true;
                    }
                }
                else if (newState?.Code == StateStageCodes.SustReprobadaFinal)
                {
                    entity.ActualDateTime = request.Dto.ActualDateTime ?? DateTimeOffset.UtcNow;
                    if (!actualDateTimeUpdated)
                    {
                        updatedProperties.Add(x => x.ActualDateTime);
                        actualDateTimeUpdated = true;
                    }
                }

                entity.IdStateStage = request.Dto.IdStateStage.Value;
                updatedProperties.Add(x => x.IdStateStage);
            }

            // Historial de observaciones: registrar solo cuando haya una observación nueva/distinta
            if (hasNewObservation)
            {
                await ObservationHistoryHelper.CreateAsync(_unitOfWork,
                    entityType: nameof(ProjectPresentation),
                    entityId: entity.Id,
                    evaluatorId: request.CurrentUser.UserId ?? 0,
                    evaluationTypeCode: EvaluationTypeCodes.ProjectPresentation,
                    observations: entity.Observations!,
                    cancellationToken);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "ProjectPresentation Id={Id} patched successfully. State={State}",
                request.Id, entity.IdStateStage);

            if (request.Dto.IdStateStage.HasValue && request.Dto.IdStateStage.Value != originalStateId)
            {
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleProjectPresentationChangeAsync(request.Id, originalStateId));
            }

            return _mapper.Map<ProjectPresentationDto>(entity);
        }
    }
}
