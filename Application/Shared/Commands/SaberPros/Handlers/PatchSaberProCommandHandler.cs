using Application.Common.Services;
using Application.Shared.DTOs.SaberPros;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.SaberPros.Handlers
{
    public class PatchSaberProCommandHandler : IRequestHandler<PatchSaberProCommand, SaberProDto>
    {
        private readonly IRepository<SaberPro, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PatchSaberProCommandHandler> _logger;

        public PatchSaberProCommandHandler(
            IRepository<SaberPro, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PatchSaberProCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<SaberProDto> Handle(PatchSaberProCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"SaberPro with ID {request.Id} not found.");

            _logger.LogInformation("Patching SaberPro Id={Id}. StateStage={State}, Observations={Obs}",
                request.Id, request.Dto.IdStateStage, request.Dto.Observations);

            var updatedProperties = new List<Expression<Func<SaberPro, object?>>>();

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
                    entityType: nameof(SaberPro),
                    entityId: entity.Id,
                    evaluatorId: request.CurrentUser.UserId ?? 0,
                    evaluationTypeCode: EvaluationTypeCodes.SaberPro,
                    observations: request.Dto.Observations,
                    cancellationToken);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            return _mapper.Map<SaberProDto>(entity);
        }
    }
}
