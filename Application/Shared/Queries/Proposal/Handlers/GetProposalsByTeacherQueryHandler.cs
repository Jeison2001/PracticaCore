using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;

namespace Application.Shared.Queries.Proposal.Handlers
{
    public class GetProposalsByTeacherQueryHandler : IRequestHandler<GetProposalsByTeacherQuery, PaginatedResult<ProposalWithDetailsResponseDto>>
    {
        private readonly ILogger<GetProposalsByTeacherQueryHandler> _logger;
        private readonly IProposalRepository _proposalRepository;
        private readonly IMapper _mapper;

        public GetProposalsByTeacherQueryHandler(
            ILogger<GetProposalsByTeacherQueryHandler> logger,
            IProposalRepository proposalRepository,
            IMapper mapper)
        {
            _logger = logger;
            _proposalRepository = proposalRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<ProposalWithDetailsResponseDto>> Handle(
            GetProposalsByTeacherQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Obtener las propuestas con una consulta optimizada en el repositorio
                var proposalsWithDetailsPaginated = await _proposalRepository.GetProposalsByTeacherWithDetailsPaginatedAsync(
                    request.TeacherId,
                    request.PageNumber,
                    request.PageSize,
                    request.SortBy,
                    request.IsDescending,
                    request.Filters,
                    request.StatusFilter,
                    cancellationToken);

                if (!proposalsWithDetailsPaginated.Items.Any())
                {
                    return new PaginatedResult<ProposalWithDetailsResponseDto>
                    {
                        Items = new List<ProposalWithDetailsResponseDto>(),
                        TotalRecords = 0,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize
                    };
                }

                // Mapear a DTOs
                var resultItems = new List<ProposalWithDetailsResponseDto>();
                
                foreach (var proposalWithDetails in proposalsWithDetailsPaginated.Items)
                {
                    var proposal = proposalWithDetails.Proposal;
                    
                    // Mapear estudiantes a DTOs
                    var studentDtos = new List<UserInscriptionModalityDto>();
                    foreach (var student in proposalWithDetails.UserInscriptionModalities)
                    {
                        studentDtos.Add(new UserInscriptionModalityDto
                        {
                            Id = student.Id,
                            IdInscriptionModality = student.IdInscriptionModality,
                            IdUser = student.IdUser,
                            UserName = student.User?.FirstName + " " + student.User?.LastName,
                            Identification = student.User?.Identification ?? "",
                            Email = student.User?.Email ?? "",
                            CurrentAcademicPeriod = student.User?.CurrentAcademicPeriod ?? "",
                            CumulativeAverage = student.User?.CumulativeAverage,
                            ApprovedCredits = student.User?.ApprovedCredits,
                            TotalAcademicCredits = student.User?.TotalAcademicCredits
                        });
                    }

                    // Crear DTO de propuesta con detalles
                    resultItems.Add(new ProposalWithDetailsResponseDto
                    {
                        Proposal = _mapper.Map<ProposalDto>(proposal),
                        StateProposalName = proposal.StateProposal?.Name ?? string.Empty,
                        ResearchLineName = proposal.ResearchLine?.Name ?? string.Empty,
                        ResearchSubLineName = proposal.ResearchSubLine?.Name ?? string.Empty,
                        Students = studentDtos
                    });
                }

                // Devolver resultado paginado
                return new PaginatedResult<ProposalWithDetailsResponseDto>
                {
                    Items = resultItems,
                    TotalRecords = proposalsWithDetailsPaginated.TotalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated proposals by teacher ID {TeacherId}: {Message}", request.TeacherId, ex.Message);
                throw;
            }
        }
    }
}