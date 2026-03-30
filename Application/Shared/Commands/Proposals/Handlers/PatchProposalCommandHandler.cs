using Application.Shared.Commands.Proposals;
using Application.Shared.DTOs.Proposals;
using Application.Common.Services.Jobs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.Proposals.Handlers
{
    public class PatchProposalCommandHandler : IRequestHandler<PatchProposalCommand, ProposalDto>
    {
        private readonly IRepository<Proposal, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<PatchProposalCommandHandler> _logger;

        public PatchProposalCommandHandler(
            IRepository<Proposal, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJobEnqueuer jobEnqueuer,
            ILogger<PatchProposalCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<ProposalDto> Handle(PatchProposalCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"Proposal with ID {request.Id} not found.");

            var originalStateId = entity.IdStateStage;

            _logger.LogInformation(
                "Patching Proposal Id={Id}. Changes: StateStage={State}, Observation={Obs}",
                request.Id,
                request.Dto.IdStateStage,
                request.Dto.Observation);

            var updatedProperties = new List<Expression<Func<Proposal, object?>>>();

            // Always update tracking fields FIRST
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTime.UtcNow;
            updatedProperties.Add(x => x.IdUserUpdatedAt);
            updatedProperties.Add(x => x.UpdatedAt);

            // Update state if provided
            if (request.Dto.IdStateStage.HasValue)
            {
                entity.IdStateStage = request.Dto.IdStateStage.Value;
                updatedProperties.Add(x => x.IdStateStage);
            }

            // Update observation if provided
            if (request.Dto.Observation != null)
            {
                entity.Observation = request.Dto.Observation;
                updatedProperties.Add(x => x.Observation);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Proposal Id={Id} patched successfully. State={State}",
                request.Id, entity.IdStateStage);

            // Trigger notification if state changed
            if (request.Dto.IdStateStage.HasValue && request.Dto.IdStateStage.Value != originalStateId)
            {
                _logger.LogInformation(
                    "Proposal state changed from {OldState} to {NewState}. Enqueuing notification job.",
                    originalStateId, request.Dto.IdStateStage.Value);

                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x =>
                    x.HandleProposalChangeAsync(request.Id, originalStateId));
            }

            return _mapper.Map<ProposalDto>(entity);
        }
    }
}
