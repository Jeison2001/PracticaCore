using Application.Common.Services;
using Application.Common.Services.Jobs;
using Domain.Interfaces.Services.Jobs;
using Application.Shared.DTOs.ScientificArticles;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.ScientificArticles.Handlers
{
    public class PatchScientificArticleCommandHandler : IRequestHandler<PatchScientificArticleCommand, ScientificArticleDto>
    {
        private readonly IRepository<ScientificArticle, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<PatchScientificArticleCommandHandler> _logger;

        public PatchScientificArticleCommandHandler(
            IRepository<ScientificArticle, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJobEnqueuer jobEnqueuer,
            ILogger<PatchScientificArticleCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobEnqueuer = jobEnqueuer;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<ScientificArticleDto> Handle(PatchScientificArticleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"ScientificArticle with ID {request.Id} not found.");

            var originalStateId = entity.IdStateStage;

            _logger.LogInformation("Patching ScientificArticle Id={Id}. StateStage={State}, Observations={Obs}",
                request.Id, request.Dto.IdStateStage, request.Dto.Observations);

            var updatedProperties = new List<Expression<Func<ScientificArticle, object?>>>();

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
                    entityType: nameof(ScientificArticle),
                    entityId: entity.Id,
                    evaluatorId: request.CurrentUser.UserId ?? 0,
                    evaluationTypeCode: EvaluationTypeCodes.ScientificArticle,
                    observations: request.Dto.Observations,
                    cancellationToken);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            if (request.Dto.IdStateStage.HasValue && request.Dto.IdStateStage.Value != originalStateId)
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleScientificArticleChangeAsync(request.Id, originalStateId));

            return _mapper.Map<ScientificArticleDto>(entity);
        }
    }
}
