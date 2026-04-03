using Application.Common.Services.Jobs;
using Application.Shared.DTOs.ProjectFinals;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.ProjectFinals.Handlers
{
    /// <summary>
    /// Actualización parcial de ProjectFinal: solo IdStateStage, ReportApprovalDate,
    /// FinalPhaseApprovalDate y Observations pueden ser modificados.
    /// Encola HandleProjectFinalChangeAsync cuando el estado cambia,
    /// que dispara notificaciones PROJECT_FINAL_APPROVED o PROJECT_FINAL_REJECTED según el nuevo estado.
    /// </summary>
    public class PatchProjectFinalCommandHandler : IRequestHandler<PatchProjectFinalCommand, ProjectFinalDto>
    {
        private readonly IRepository<ProjectFinal, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<PatchProjectFinalCommandHandler> _logger;

        public PatchProjectFinalCommandHandler(
            IRepository<ProjectFinal, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJobEnqueuer jobEnqueuer,
            ILogger<PatchProjectFinalCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<ProjectFinalDto> Handle(PatchProjectFinalCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"ProjectFinal with ID {request.Id} not found.");

            var originalStateId = entity.IdStateStage;

            _logger.LogInformation(
                "Patching ProjectFinal Id={Id}. Changes: StateStage={State}, ReportApprovalDate={RDate}, FinalPhaseApprovalDate={FDate}, Observations={Obs}",
                request.Id,
                request.Dto.IdStateStage,
                request.Dto.ReportApprovalDate,
                request.Dto.FinalPhaseApprovalDate,
                request.Dto.Observations);

            var updatedProperties = new List<Expression<Func<ProjectFinal, object?>>>();

            // Siempre actualizar campos de tracking primero
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            updatedProperties.Add(x => x.IdUserUpdatedAt);
            updatedProperties.Add(x => x.UpdatedAt);

            // Actualizar estado si se provee
            if (request.Dto.IdStateStage.HasValue)
            {
                entity.IdStateStage = request.Dto.IdStateStage.Value;
                updatedProperties.Add(x => x.IdStateStage);
            }

            // Actualizar fecha de aprobación del informe si se provee
            if (request.Dto.ReportApprovalDate.HasValue)
            {
                entity.ReportApprovalDate = request.Dto.ReportApprovalDate.Value;
                updatedProperties.Add(x => x.ReportApprovalDate);
            }

            // Actualizar fecha de aprobación de la fase final si se provee
            if (request.Dto.FinalPhaseApprovalDate.HasValue)
            {
                entity.FinalPhaseApprovalDate = request.Dto.FinalPhaseApprovalDate.Value;
                updatedProperties.Add(x => x.FinalPhaseApprovalDate);
            }

            // Actualizar observaciones si se proveen
            if (request.Dto.Observations != null)
            {
                entity.Observations = request.Dto.Observations;
                updatedProperties.Add(x => x.Observations);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "ProjectFinal Id={Id} patched successfully. State={State}",
                request.Id, entity.IdStateStage);

            // Disparar notificación si el estado cambió
            if (request.Dto.IdStateStage.HasValue && request.Dto.IdStateStage.Value != originalStateId)
            {
                _logger.LogInformation(
                    "ProjectFinal state changed from {OldState} to {NewState}. Enqueuing notification job.",
                    originalStateId, request.Dto.IdStateStage.Value);

                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x =>
                    x.HandleProjectFinalChangeAsync(request.Id, originalStateId));
            }

            return _mapper.Map<ProjectFinalDto>(entity);
        }
    }
}
