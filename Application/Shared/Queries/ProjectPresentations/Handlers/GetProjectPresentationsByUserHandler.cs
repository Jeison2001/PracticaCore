using Application.Shared.DTOs.ProjectFinals;
using Application.Shared.DTOs.ProjectPresentations;
using Application.Shared.DTOs.Proposals;
using Application.Shared.DTOs.StateStages;
using Application.Shared.DTOs.UserInscriptionModalities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.ProjectPresentations.Handlers
{
    public class GetProjectPresentationsByUserHandler : IRequestHandler<GetProjectPresentationsByUserQuery, List<ProjectPresentationWithDetailsResponseDto>>
    {
        private readonly IProjectPresentationRepository _repository;

        public GetProjectPresentationsByUserHandler(IProjectPresentationRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ProjectPresentationWithDetailsResponseDto>> Handle(GetProjectPresentationsByUserQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByUserIdWithProposalAndStudentsAsync(request.UserId, request.Status);

            return entities
                .OrderByDescending(e => e.Presentation.CreatedAt)
                .ThenByDescending(e => e.Presentation.Id)
                .Select(e =>
                {
                    var studentsDto = e.Students.Select(uim => new UserInscriptionModalityDto
                    {
                        Id = uim.Id,
                        IdInscriptionModality = uim.IdInscriptionModality,
                        IdUser = uim.IdUser,
                        UserName = uim.User != null ? $"{uim.User.FirstName} {uim.User.LastName}".Trim() : string.Empty,
                        Identification = uim.User?.Identification ?? string.Empty,
                        Email = uim.User?.Email ?? string.Empty
                    }).ToList();

                    return new ProjectPresentationWithDetailsResponseDto
                    {
                        ProjectPresentation = new ProjectPresentationDetailsDto
                        {
                            Id = e.Presentation.Id,
                            IdInscriptionModality = e.Presentation.IdInscriptionModality,
                            IdStateStage = e.Presentation.IdStateStage,
                            ScheduledDateTime = e.Presentation.ScheduledDateTime,
                            ActualDateTime = e.Presentation.ActualDateTime,
                            LocationOrLink = e.Presentation.LocationOrLink,
                            PresentationModality = e.Presentation.PresentationModality,
                            AttemptNumber = e.Presentation.AttemptNumber,
                            Observations = e.Presentation.Observations,
                            IdUserCreatedAt = e.Presentation.IdUserCreatedAt ?? 0,
                            CreatedAt = e.Presentation.CreatedAt,
                            IdUserUpdatedAt = e.Presentation.IdUserUpdatedAt,
                            UpdatedAt = e.Presentation.UpdatedAt,
                            OperationRegister = e.Presentation.OperationRegister ?? string.Empty,
                            StatusRegister = e.Presentation.StatusRegister,
                            StateStage = e.Presentation.StateStage != null ? new StateStageDto
                            {
                                Id = e.Presentation.StateStage.Id,
                                Code = e.Presentation.StateStage.Code,
                                Name = e.Presentation.StateStage.Name,
                                Description = e.Presentation.StateStage.Description,
                                IsInitialState = e.Presentation.StateStage.IsInitialState,
                                IsFinalStateForStage = e.Presentation.StateStage.IsFinalStateForStage,
                                IsFinalStateForModalityOverall = e.Presentation.StateStage.IsFinalStateForModalityOverall,
                                IsRejectionState = e.Presentation.StateStage.IsRejectionState
                            } : null
                        },
                        ProjectFinal = e.ProjectFinal != null ? new ProjectFinalDetailsDto
                        {
                            Id = e.ProjectFinal.Id,
                            IdStateStage = e.ProjectFinal.IdStateStage,
                            ReportApprovalDate = e.ProjectFinal.ReportApprovalDate,
                            FinalPhaseApprovalDate = e.ProjectFinal.FinalPhaseApprovalDate,
                            Observations = e.ProjectFinal.Observations,
                            IdUserCreatedAt = e.ProjectFinal.IdUserCreatedAt ?? 0,
                            CreatedAt = e.ProjectFinal.CreatedAt,
                            IdUserUpdatedAt = e.ProjectFinal.IdUserUpdatedAt,
                            UpdatedAt = e.ProjectFinal.UpdatedAt,
                            OperationRegister = e.ProjectFinal.OperationRegister ?? string.Empty,
                            StatusRegister = e.ProjectFinal.StatusRegister,
                            StateStage = e.ProjectFinal.StateStage != null ? new StateStageDto
                            {
                                Id = e.ProjectFinal.StateStage.Id,
                                Code = e.ProjectFinal.StateStage.Code,
                                Name = e.ProjectFinal.StateStage.Name,
                                Description = e.ProjectFinal.StateStage.Description
                            } : null
                        } : null,
                        Proposal = new ProposalWithDetailsResponseDto
                        {
                            Proposal = new ProposalDto
                            {
                                Id = e.Proposal.Id,
                                Title = e.Proposal.Title,
                                Description = e.Proposal.Description,
                                GeneralObjective = e.Proposal.GeneralObjective,
                                SpecificObjectives = e.Proposal.SpecificObjectives,
                                IdResearchLine = e.Proposal.IdResearchLine,
                                IdResearchSubLine = e.Proposal.IdResearchSubLine,
                                IdStateStage = e.Proposal.IdStateStage
                            },
                            StateStageName = e.Proposal.StateStage?.Name ?? string.Empty,
                            StateStageCode = e.Proposal.StateStage?.Code,
                            ResearchLineName = e.Proposal.ResearchLine?.Name ?? string.Empty,
                            ResearchSubLineName = e.Proposal.ResearchSubLine?.Name ?? string.Empty,
                            Students = studentsDto
                        },
                        Students = studentsDto
                    };
                }).ToList();
        }
    }
}
