using Application.Shared.DTOs.ProjectFinal;
using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.StateProjectFinal;
using Application.Shared.Queries.ProjectFinal;
using Domain.Common;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.ProjectFinal.Handlers
{
    public class GetAllProjectFinalsHandler : IRequestHandler<GetAllProjectFinalsQuery, PaginatedResult<ProjectFinalWithDetailsResponseDto>>
    {
        private readonly IProjectFinalRepository _repository;
        public GetAllProjectFinalsHandler(IProjectFinalRepository repository)
        {
            _repository = repository;
        }
        public async Task<PaginatedResult<ProjectFinalWithDetailsResponseDto>> Handle(GetAllProjectFinalsQuery request, CancellationToken cancellationToken)
        {
            var pagedResult = await _repository.GetAllWithProposalAndStudentsAsync(request.PageNumber, request.PageSize, request.SortBy, request.IsDescending, request.Filters);
            return new PaginatedResult<ProjectFinalWithDetailsResponseDto>
            {
                Items = pagedResult.Items.Select(e => new ProjectFinalWithDetailsResponseDto
                {
                    ProjectFinal = new ProjectFinalDetailsDto
                    {
                        Id = e.Project.Id,
                        IdStateProjectFinal = e.Project.IdStateProjectFinal,
                        ReportApprovalDate = e.Project.ReportApprovalDate,
                        FinalPhaseApprovalDate = e.Project.FinalPhaseApprovalDate,
                        Observations = e.Project.Observations,
                        StateProjectFinal = e.Project.StateProjectFinal != null ? new StateProjectFinalDto
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
                }).ToList(),
                TotalRecords = pagedResult.TotalRecords,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
        }
    }
}
