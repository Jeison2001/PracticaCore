using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Queries.Proposal.Handlers
{
    public class GetAllProposalsHandler : IRequestHandler<GetAllProposalsQuery, PaginatedResult<ProposalWithDetailsResponseDto>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetAllProposalsHandler> _logger;
        private readonly IRepository<Domain.Entities.Proposal, int> _proposalRepository;
        private readonly IRepository<StateProposal, int> _stateProposalRepository;
        private readonly IRepository<ResearchLine, int> _researchLineRepository;
        private readonly IRepository<ResearchSubLine, int> _researchSubLineRepository;
        private readonly IRepository<UserInscriptionModality, int> _userInscriptionModalityRepository;
        private readonly IRepository<InscriptionModality, int> _inscriptionModalityRepository;
        private readonly IRepository<User, int> _userRepository;

        public GetAllProposalsHandler(
            IMediator mediator,
            ILogger<GetAllProposalsHandler> logger,
            IRepository<Domain.Entities.Proposal, int> proposalRepository,
            IRepository<StateProposal, int> stateProposalRepository,
            IRepository<ResearchLine, int> researchLineRepository,
            IRepository<ResearchSubLine, int> researchSubLineRepository,
            IRepository<UserInscriptionModality, int> userInscriptionModalityRepository,
            IRepository<InscriptionModality, int> inscriptionModalityRepository,
            IRepository<User, int> userRepository)
        {
            _mediator = mediator;
            _logger = logger;
            _proposalRepository = proposalRepository;
            _stateProposalRepository = stateProposalRepository;
            _researchLineRepository = researchLineRepository;
            _researchSubLineRepository = researchSubLineRepository;
            _userInscriptionModalityRepository = userInscriptionModalityRepository;
            _inscriptionModalityRepository = inscriptionModalityRepository;
            _userRepository = userRepository;
        }

        public async Task<PaginatedResult<ProposalWithDetailsResponseDto>> Handle(
            GetAllProposalsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Obtener todas las propuestas con paginación y filtros
                var getAllProposalsQuery = new GetAllEntitiesQuery<Domain.Entities.Proposal, int, ProposalDto>
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    IsDescending = request.IsDescending,
                    Filters = request.Filters
                };

                var proposalsResult = await _mediator.Send(getAllProposalsQuery, cancellationToken);
                var proposals = proposalsResult.Items.ToList();

                if (proposals.Count == 0)
                {
                    return new PaginatedResult<ProposalWithDetailsResponseDto>
                    {
                        Items = new List<ProposalWithDetailsResponseDto>(),
                        TotalRecords = 0,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize
                    };
                }

                // 2. Obtener los IDs de las propuestas para obtener las modalidades de inscripción relacionadas
                var proposalIds = proposals.Select(p => p.Id).ToList();

                // En este modelo, el Id de Proposal es el mismo que el de InscriptionModality
                // 3. Obtener las modalidades de inscripción con los mismos IDs que las propuestas
                var inscriptionModalities = await _inscriptionModalityRepository.GetAllAsync(
                    filter: im => proposalIds.Contains(im.Id));

                // 4. Obtener los estudiantes asociados a estas modalidades de inscripción
                var inscriptionModalityIds = inscriptionModalities.Select(im => im.Id).ToList();
                var students = await _userInscriptionModalityRepository.GetAllAsync(
                    filter: uim => inscriptionModalityIds.Contains(uim.IdInscriptionModality));

                // 5. Agrupar estudiantes por IdInscriptionModality (que es equivalente a IdProposal)
                var studentsByInscriptionModality = students
                    .GroupBy(s => s.IdInscriptionModality)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 6. Obtener información relacionada (estados, líneas y sublíneas de investigación)
                var stateProposalIds = proposals.Select(p => p.IdStateProposal).Distinct().ToList();
                var researchLineIds = proposals.Select(p => p.IdResearchLine).Distinct().ToList();
                var researchSubLineIds = proposals.Select(p => p.IdResearchSubLine).Distinct().ToList();

                // Usar GetAllAsync con filtros para obtener las entidades relacionadas
                var stateProposals = await _stateProposalRepository.GetAllAsync(
                    filter: sp => stateProposalIds.Contains(sp.Id));
                
                var researchLines = await _researchLineRepository.GetAllAsync(
                    filter: rl => researchLineIds.Contains(rl.Id));
                
                var researchSubLines = await _researchSubLineRepository.GetAllAsync(
                    filter: rsl => researchSubLineIds.Contains(rsl.Id));

                var stateProposalsDict = stateProposals.ToDictionary(sp => sp.Id);
                var researchLinesDict = researchLines.ToDictionary(rl => rl.Id);
                var researchSubLinesDict = researchSubLines.ToDictionary(rsl => rsl.Id);

                // 7. Obtener información de los usuarios para los nombres
                var userIds = students.Select(s => s.IdUser).Distinct().ToList();
                var users = new Dictionary<int, User>();
                
                if (userIds.Any())
                {
                    var usersResult = await _userRepository.GetAllAsync(
                        filter: u => userIds.Contains(u.Id));
                    users = usersResult.ToDictionary(u => u.Id);
                }

                // 8. Construir la respuesta para cada propuesta
                var resultItems = new List<ProposalWithDetailsResponseDto>();
                
                foreach (var proposalDto in proposals)
                {
                    stateProposalsDict.TryGetValue(proposalDto.IdStateProposal, out var stateProposal);
                    researchLinesDict.TryGetValue(proposalDto.IdResearchLine, out var researchLine);
                    researchSubLinesDict.TryGetValue(proposalDto.IdResearchSubLine, out var researchSubLine);

                    if (stateProposal == null || researchLine == null)
                    {
                        _logger.LogWarning($"Datos faltantes para la propuesta con ID {proposalDto.Id}");
                        continue;
                    }

                    // Obtener estudiantes de esta propuesta (a través de su modalidad de inscripción)
                    studentsByInscriptionModality.TryGetValue(proposalDto.Id, out var studentsForProposal);
                    
                    // Preparar DTOs de estudiantes
                    var studentDtosForProposal = new List<UserInscriptionModalityDto>();
                    if (studentsForProposal != null)
                    {
                        foreach (var student in studentsForProposal)
                        {
                            var studentDto = new UserInscriptionModalityDto
                            {
                                Id = student.Id,
                                IdInscriptionModality = student.IdInscriptionModality,
                                IdUser = student.IdUser,
                                UserName = "Usuario no encontrado"
                            };
                            
                            // Añadir nombre del estudiante
                            if (users.TryGetValue(student.IdUser, out var user))
                            {
                                studentDto.UserName = $"{user.FirstName} {user.LastName}";
                            }
                            else
                            {
                                _logger.LogWarning($"No se encontró el usuario con Id: {student.IdUser}");
                            }
                            
                            studentDtosForProposal.Add(studentDto);
                        }
                    }

                    // Añadir la propuesta completa a la respuesta
                    resultItems.Add(new ProposalWithDetailsResponseDto
                    {
                        Proposal = proposalDto,
                        StateProposalName = stateProposal.Name,
                        ResearchLineName = researchLine.Name,
                        ResearchSubLineName = researchSubLine?.Name ?? "No aplica",
                        Students = studentDtosForProposal
                    });
                }

                // 9. Devolver el resultado paginado
                return new PaginatedResult<ProposalWithDetailsResponseDto>
                {
                    Items = resultItems,
                    TotalRecords = proposalsResult.TotalRecords,
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