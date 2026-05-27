using Application.Common.Services;
using Application.Shared.DTOs.CoTerminals;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.CoTerminals.Handlers
{
    public class PatchCoTerminalCommandHandler : IRequestHandler<PatchCoTerminalCommand, CoTerminalDto>
    {
        private readonly IRepository<CoTerminal, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PatchCoTerminalCommandHandler> _logger;

        public PatchCoTerminalCommandHandler(
            IRepository<CoTerminal, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PatchCoTerminalCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CoTerminalDto> Handle(PatchCoTerminalCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"CoTerminal with ID {request.Id} not found.");

            _logger.LogInformation("Patching CoTerminal Id={Id}. StateStage={State}, Observations={Obs}",
                request.Id, request.Dto.IdStateStage, request.Dto.Observations);

            var updatedProperties = new List<Expression<Func<CoTerminal, object?>>>();

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
                    entityType: nameof(CoTerminal),
                    entityId: entity.Id,
                    evaluatorId: request.CurrentUser.UserId ?? 0,
                    evaluationTypeCode: EvaluationTypeCodes.CoTerminal,
                    observations: request.Dto.Observations,
                    cancellationToken);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            return _mapper.Map<CoTerminalDto>(entity);
        }
    }
}
