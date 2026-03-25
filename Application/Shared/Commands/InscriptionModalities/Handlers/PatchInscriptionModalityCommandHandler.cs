using Application.Shared.Commands.InscriptionModalities;
using Application.Shared.DTOs.InscriptionModalities;
using AutoMapper;
using Domain.Entities;
using Application.Common.Services.Jobs;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.InscriptionModalities.Handlers
{
    public class PatchInscriptionModalityCommandHandler : IRequestHandler<PatchInscriptionModalityCommand, InscriptionModalityDto>
    {
        private readonly IRepository<InscriptionModality, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<PatchInscriptionModalityCommandHandler> _logger;

        public PatchInscriptionModalityCommandHandler(
            IRepository<InscriptionModality, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJobEnqueuer jobEnqueuer,
            ILogger<PatchInscriptionModalityCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<InscriptionModalityDto> Handle(PatchInscriptionModalityCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"InscriptionModality with ID {request.Id} not found.");

            var originalStateId = entity.IdStateInscription;

            _logger.LogInformation(
                "Patching InscriptionModality Id={Id}. Changes: StateInscription={State}, Observations={Obs}, ApprovalDate={Date}",
                request.Id,
                request.Dto.IdStateInscription,
                request.Dto.Observations,
                request.Dto.ApprovalDate);

            var updatedProperties = new List<Expression<Func<InscriptionModality, object?>>>();

            // Always update tracking fields FIRST (before state change to ensure domain event captures correct user)
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTime.UtcNow;
            updatedProperties.Add(x => x.IdUserUpdatedAt);
            updatedProperties.Add(x => x.UpdatedAt);

            if (request.Dto.IdStateInscription.HasValue)
            {
                entity.IdStateInscription = request.Dto.IdStateInscription.Value;
                updatedProperties.Add(x => x.IdStateInscription);
            }

            if (request.Dto.ApprovalDate.HasValue)
            {
                entity.ApprovalDate = EnsureUtc(request.Dto.ApprovalDate.Value);
                updatedProperties.Add(x => x.ApprovalDate);
            }

            if (request.Dto.Observations != null)
            {
                entity.Observations = request.Dto.Observations;
                updatedProperties.Add(x => x.Observations);
            }

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "InscriptionModality Id={Id} patched successfully. New state: {State}",
                request.Id, entity.IdStateInscription);

            // Trigger notification if state changed (matches logic in UpdateInscriptionWithStudentsHandler)
            if (request.Dto.IdStateInscription.HasValue && request.Dto.IdStateInscription.Value != originalStateId)
            {
                _logger.LogInformation(
                    "Inscription state changed from {OldState} to {NewState}. Enqueuing notification job.",
                    originalStateId, entity.IdStateInscription);
                
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => 
                    x.HandleInscriptionChangeAsync(request.Id, originalStateId));
            }

            return _mapper.Map<InscriptionModalityDto>(entity);
        }

        private static DateTime EnsureUtc(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
    }
}
