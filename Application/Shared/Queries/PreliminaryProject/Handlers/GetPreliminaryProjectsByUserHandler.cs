using Application.Shared.DTOs.PreliminaryProject;
using Application.Shared.Queries.PreliminaryProject;
using MediatR;
using Domain.Interfaces;
using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.StatePreliminaryProject;
using Application.Shared.DTOs.UserInscriptionModality;

namespace Application.Shared.Queries.PreliminaryProject.Handlers
{
    public class GetPreliminaryProjectsByUserHandler : IRequestHandler<GetPreliminaryProjectsByUserQuery, List<PreliminaryProjectWithDetailsResponseDto>>
    {
        private readonly IPreliminaryProjectRepository _repository;
        public GetPreliminaryProjectsByUserHandler(IPreliminaryProjectRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<PreliminaryProjectWithDetailsResponseDto>> Handle(GetPreliminaryProjectsByUserQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByUserIdWithProposalAndStudentsAsync(request.UserId, request.Status);
            return entities.Select(e => new PreliminaryProjectWithDetailsResponseDto
            {
                PreliminaryProject = new PreliminaryProjectDetailsDto
                {
                    Id = e.Project.Id,
                    IdStatePreliminaryProject = e.Project.IdStatePreliminaryProject,
                    ApprovalDate = e.Project.ApprovalDate,
                    Observations = e.Project.Observations,
                    StatePreliminaryProject = e.Project.StatePreliminaryProject != null ? new StatePreliminaryProjectDto
                    {
                        Id = e.Project.StatePreliminaryProject.Id,
                        Code = e.Project.StatePreliminaryProject.Code,
                        Name = e.Project.StatePreliminaryProject.Name,
                        Description = e.Project.StatePreliminaryProject.Description
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
                    Students = e.Students.Select(uim => new UserInscriptionModalityDto
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
