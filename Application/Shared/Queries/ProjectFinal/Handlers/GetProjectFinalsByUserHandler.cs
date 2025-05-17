using Domain.Interfaces;
using Application.Shared.DTOs.ProjectFinal;
using Application.Shared.DTOs.Proposal;
using Application.Shared.Queries.ProjectFinal;
using MediatR;

namespace Application.Shared.Queries.ProjectFinal.Handlers
{
    public class GetProjectFinalsByUserHandler : IRequestHandler<GetProjectFinalsByUserQuery, List<ProjectFinalWithDetailsResponseDto>>
    {
        private readonly IProjectFinalRepository _repository;
        public GetProjectFinalsByUserHandler(IProjectFinalRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<ProjectFinalWithDetailsResponseDto>> Handle(GetProjectFinalsByUserQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByUserIdWithProposalAndStudentsAsync(request.UserId, request.Status);
            return entities.Select(e => new ProjectFinalWithDetailsResponseDto
            {
                ProjectFinal = new ProjectFinalDetailsDto
                {
                    Id = e.Project.Id,
                    IdStateProjectFinal = e.Project.IdStateProjectFinal,
                    ReportApprovalDate = e.Project.ReportApprovalDate,
                    FinalPhaseApprovalDate = e.Project.FinalPhaseApprovalDate,
                    Observations = e.Project.Observations,
                    StateProjectFinal = e.Project.StateProjectFinal != null ? new Application.Shared.DTOs.StateProjectFinal.StateProjectFinalDto
                    {
                        Id = e.Project.StateProjectFinal.Id,
                        Code = e.Project.StateProjectFinal.Code,
                        Name = e.Project.StateProjectFinal.Name,
                        Description = e.Project.StateProjectFinal.Description
                    } : null
                },
                Proposal = new ProposalWithDetailsResponseDto
                {
                    Proposal = new ProposalDto
                    {
                        Id = e.Proposal.Id,
                        Title = e.Proposal.Title,
                        Description = e.Proposal.Description,
                        IdResearchLine = e.Proposal.IdResearchLine,
                        IdResearchSubLine = e.Proposal.IdResearchSubLine,
                        IdStateProposal = e.Proposal.IdStateProposal
                    },
                    StateProposalName = e.Proposal.StateProposal?.Name ?? string.Empty,
                    ResearchLineName = e.Proposal.ResearchLine?.Name ?? string.Empty,
                    ResearchSubLineName = e.Proposal.ResearchSubLine?.Name ?? string.Empty,
                    Students = e.Students.Select(uim => new Application.Shared.DTOs.UserInscriptionModality.UserInscriptionModalityDto
                    {
                        Id = uim.Id,
                        IdInscriptionModality = uim.IdInscriptionModality,
                        IdUser = uim.IdUser,
                        UserName = uim.User != null ? (uim.User.FirstName + " " + uim.User.LastName) : string.Empty,
                        Identification = uim.User?.Identification ?? string.Empty,
                        Email = uim.User?.Email ?? string.Empty,
                        CurrentAcademicPeriod = string.Empty, // Completar si tienes el dato
                        CumulativeAverage = null, // Completar si tienes el dato
                        ApprovedCredits = null, // Completar si tienes el dato
                        TotalAcademicCredits = null // Completar si tienes el dato
                    }).ToList()
                }
            }).ToList();
        }
    }
}
