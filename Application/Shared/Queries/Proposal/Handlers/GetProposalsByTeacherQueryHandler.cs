using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
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
    public class GetProposalsByTeacherQueryHandler : IRequestHandler<GetProposalsByTeacherQuery, List<ProposalWithDetailsResponseDto>>
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

        public async Task<List<ProposalWithDetailsResponseDto>> Handle(
            GetProposalsByTeacherQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Obtener las propuestas con una consulta optimizada en el repositorio
                var proposalsWithDetails = await _proposalRepository.GetProposalsByTeacherWithDetailsAsync(
                    request.TeacherId, 
                    request.StatusFilter, 
                    cancellationToken);

                if (!proposalsWithDetails.Any())
                {
                    return new List<ProposalWithDetailsResponseDto>();
                }

                // Mapear a DTOs
                var result = new List<ProposalWithDetailsResponseDto>();
                
                foreach (var proposalWithDetails in proposalsWithDetails)
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
                    result.Add(new ProposalWithDetailsResponseDto
                    {
                        Proposal = _mapper.Map<ProposalDto>(proposal),
                        StateProposalName = proposal.StateProposal?.Name ?? string.Empty,
                        ResearchLineName = proposal.ResearchLine?.Name ?? string.Empty,
                        ResearchSubLineName = proposal.ResearchSubLine?.Name ?? string.Empty,
                        Students = studentDtos
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposals by teacher ID {TeacherId}: {Message}", request.TeacherId, ex.Message);
                throw;
            }
        }
    }
}