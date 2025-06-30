using Application.Shared.DTOs.PreliminaryProject;
using Application.Shared.Queries.PreliminaryProject;
using Domain.Common;
using MediatR;
using Domain.Interfaces;
using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.StateStage;
using Application.Shared.DTOs.UserInscriptionModality;
using System.Linq;

namespace Application.Shared.Queries.PreliminaryProject.Handlers
{
    public class GetPreliminaryProjectsByTeacherHandler : IRequestHandler<GetPreliminaryProjectsByTeacherQuery, PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>
    {
        private readonly IPreliminaryProjectRepository _repository;
        public GetPreliminaryProjectsByTeacherHandler(IPreliminaryProjectRepository repository)
        {
            _repository = repository;
        }
        public async Task<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>> Handle(GetPreliminaryProjectsByTeacherQuery request, CancellationToken cancellationToken)
        {
            var pagedResult = await _repository.GetByTeacherIdWithProposalAndStudentsAsync(request.TeacherId, request.PageNumber, request.PageSize, request.SortBy, request.IsDescending, request.Filters);
            return new PaginatedResult<PreliminaryProjectWithDetailsResponseDto>
            {
                Items = pagedResult.Items.Select(e => new PreliminaryProjectWithDetailsResponseDto
                {                    PreliminaryProject = new PreliminaryProjectDetailsDto
                    {
                        Id = e.Project.Id,
                        IdStateStage = e.Project.IdStateStage,
                        ApprovalDate = e.Project.ApprovalDate,
                        Observations = e.Project.Observations,
                        IdUserCreatedAt = e.Project.IdUserCreatedAt ?? 0,
                        CreatedAt = e.Project.CreatedAt,
                        IdUserUpdatedAt = e.Project.IdUserUpdatedAt,
                        UpdatedAt = e.Project.UpdatedAt,
                        OperationRegister = e.Project.OperationRegister ?? string.Empty,
                        StatusRegister = e.Project.StatusRegister,
                        StateStage = e.Project.StateStage != null ? new StateStageDto
                        {
                            Id = e.Project.StateStage.Id,
                            Code = e.Project.StateStage.Code,
                            Name = e.Project.StateStage.Name,
                            Description = e.Project.StateStage.Description
                        } : null
                    },
                    Proposal = new ProposalWithDetailsResponseDto
                    {                        Proposal = new ProposalDto
                        {
                            Id = e.Proposal.Id,
                            Title = e.Proposal.Title,
                            Description = e.Proposal.Description,
                            IdResearchLine = e.Proposal.IdResearchLine,
                            IdResearchSubLine = e.Proposal.IdResearchSubLine,
                            IdStateStage = e.Proposal.IdStateStage
                        },
                        StateStageName = e.Proposal.StateStage?.Name ?? string.Empty,
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
