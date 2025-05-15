using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Queries.Proposal.Handlers
{
    public class GetAllProposalsHandler : IRequestHandler<GetAllProposalsQuery, PaginatedResult<ProposalWithDetailsResponseDto>>
    {
        private readonly ILogger<GetAllProposalsHandler> _logger;
        private readonly IProposalRepository _proposalRepository;
        private readonly IMapper _mapper;

        public GetAllProposalsHandler(
            ILogger<GetAllProposalsHandler> logger,
            IProposalRepository proposalRepository,
            IMapper mapper)
        {
            _logger = logger;
            _proposalRepository = proposalRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<ProposalWithDetailsResponseDto>> Handle(
            GetAllProposalsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Utilizar el método del repositorio específico para obtener propuestas con sus detalles
                var result = await _proposalRepository.GetAllProposalsWithDetailsPaginatedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.SortBy ?? "",
                    request.IsDescending,
                    request.Filters ?? new Dictionary<string, string>(),
                    null,
                    cancellationToken);

                if (!result.Items.Any())
                {
                    return new PaginatedResult<ProposalWithDetailsResponseDto>
                    {
                        Items = new List<ProposalWithDetailsResponseDto>(),
                        TotalRecords = 0,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize
                    };
                }

                // Transformar el resultado en DTOs
                var resultItems = new List<ProposalWithDetailsResponseDto>();
                
                foreach (var item in result.Items)
                {
                    var proposalDto = _mapper.Map<ProposalDto>(item.Proposal);
                    
                    // Preparar DTOs de estudiantes
                    var studentDtosForProposal = new List<UserInscriptionModalityDto>();
                    
                    foreach (var uim in item.UserInscriptionModalities)
                    {
                        var studentDto = new UserInscriptionModalityDto
                        {
                            IdInscriptionModality = uim.IdInscriptionModality,
                            IdUser = uim.IdUser,
                            UserName = uim.User?.FirstName + " " + uim.User?.LastName,
                            Email = uim.User?.Email ?? string.Empty
                        };
                        
                        studentDtosForProposal.Add(studentDto);
                    }

                    // Añadir la propuesta completa a la respuesta
                    resultItems.Add(new ProposalWithDetailsResponseDto
                    {
                        Proposal = proposalDto,
                        StateProposalName = item.Proposal.StateProposal?.Name ?? string.Empty,
                        ResearchLineName = item.Proposal.ResearchLine?.Name ?? string.Empty,
                        ResearchSubLineName = item.Proposal.ResearchSubLine?.Name ?? "No aplica",
                        Students = studentDtosForProposal
                    });
                }

                // Devolver el resultado paginado
                return new PaginatedResult<ProposalWithDetailsResponseDto>
                {
                    Items = resultItems,
                    TotalRecords = result.TotalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener propuestas con detalles: {Message}", ex.Message);
                throw;
            }
        }
    }
}