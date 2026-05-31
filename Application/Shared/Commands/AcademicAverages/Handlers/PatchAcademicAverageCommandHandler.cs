using Application.Common.Services;
using Application.Common.Services.Jobs;
using Application.Shared.DTOs.AcademicAverages;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.AcademicAverages.Handlers
{
    public class PatchAcademicAverageCommandHandler : IRequestHandler<PatchAcademicAverageCommand, AcademicAverageDto>
    {
        private readonly IRepository<AcademicAverage, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<PatchAcademicAverageCommandHandler> _logger;

        public PatchAcademicAverageCommandHandler(
            IRepository<AcademicAverage, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJobEnqueuer jobEnqueuer,
            ILogger<PatchAcademicAverageCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<AcademicAverageDto> Handle(PatchAcademicAverageCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"AcademicAverage with ID {request.Id} not found.");

            var originalStateId = entity.IdStateStage;

            _logger.LogInformation("Patching AcademicAverage Id={Id}. StateStage={State}, Observations={Obs}",
                request.Id, request.Dto.IdStateStage, request.Dto.Observations);

            var updatedProperties = new List<Expression<Func<AcademicAverage, object?>>>();

            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            updatedProperties.Add(x => x.IdUserUpdatedAt);
            updatedProperties.Add(x => x.UpdatedAt);

            if (request.Dto.IdStateStage.HasValue)
            {
                entity.IdStateStage = request.Dto.IdStateStage.Value;
                updatedProperties.Add(x => x.IdStateStage);
            }

            if (request.Dto.Observations != null)
            {
                entity.Observations = request.Dto.Observations;
                updatedProperties.Add(x => x.Observations);
            }

            if (!string.IsNullOrWhiteSpace(request.Dto.Observations))
            {
                await ObservationHistoryHelper.CreateAsync(
                    _unitOfWork,
                    entityType: nameof(AcademicAverage),
                    entityId: entity.Id,
                    evaluatorId: request.CurrentUser.UserId ?? 0,
                    evaluationTypeCode: EvaluationTypeCodes.AcademicAverage,
                    observations: request.Dto.Observations,
                    cancellationToken);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            if (request.Dto.IdStateStage.HasValue && request.Dto.IdStateStage.Value != originalStateId)
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleAcademicAverageChangeAsync(request.Id, originalStateId));

            return _mapper.Map<AcademicAverageDto>(entity);
        }
    }
}
