using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.Proposal.Handlers
{
    public class GetProposalsByUserQueryHandler : IRequestHandler<GetProposalsByUserQuery, List<ProposalWithDetailsResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private static readonly int ProcessorCount = Math.Max(1, Environment.ProcessorCount / 2);

        public GetProposalsByUserQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<ProposalWithDetailsResponseDto>> Handle(GetProposalsByUserQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtenemos los repositorios necesarios
            var proposalRepo = _unitOfWork.GetRepository<Domain.Entities.Proposal, int>();
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var stateProposalRepo = _unitOfWork.GetRepository<StateProposal, int>();
            var researchLineRepo = _unitOfWork.GetRepository<ResearchLine, int>();
            var researchSubLineRepo = _unitOfWork.GetRepository<ResearchSubLine, int>();
            
            // 2. Obtenemos todas las inscripciones del usuario de forma segura
            var userInscriptionModalities = await userInscriptionRepo.GetAllAsync(
                filter: uim => uim.IdUser == request.UserId);

            if (!userInscriptionModalities.Any())
            {
                return new List<ProposalWithDetailsResponseDto>();
            }

            // 3. Extraemos los IDs de InscriptionModality
            var inscriptionModalityIds = userInscriptionModalities.Select(uim => uim.IdInscriptionModality).Distinct().ToList();
            
            // 4. Obtenemos las propuestas relacionadas a esas inscripciones
            var proposals = await proposalRepo.GetAllAsync(
                filter: p => inscriptionModalityIds.Contains(p.Id));

            if (!proposals.Any())
            {
                return new List<ProposalWithDetailsResponseDto>();
            }

            // 5. Obtenemos todos los IDs necesarios para minimizar consultas
            var proposalsList = proposals.ToList();
            var stateProposalIds = proposalsList.Select(p => p.IdStateProposal).Distinct().ToList();
            var researchLineIds = proposalsList.Select(p => p.IdResearchLine).Distinct().ToList();
            var researchSubLineIds = proposalsList.Select(p => p.IdResearchSubLine).Distinct().ToList();
            var userIds = userInscriptionModalities.Select(uim => uim.IdUser).Distinct().ToList();

            // 6. Obtenemos todos los datos relacionados secuencialmente para evitar problemas con el DbContext
            var stateProposals = await stateProposalRepo.GetAllAsync(
                filter: sp => stateProposalIds.Contains(sp.Id));
            var researchLines = await researchLineRepo.GetAllAsync(
                filter: rl => researchLineIds.Contains(rl.Id));
            var researchSubLines = await researchSubLineRepo.GetAllAsync(
                filter: rsl => researchSubLineIds.Contains(rsl.Id));
            var users = await userRepo.GetAllAsync(
                filter: u => userIds.Contains(u.Id));

            // 7. Creamos diccionarios para acceso eficiente
            var stateProposalsDict = stateProposals.ToDictionary(sp => sp.Id, sp => sp);
            var researchLinesDict = researchLines.ToDictionary(rl => rl.Id, rl => rl);
            var researchSubLinesDict = researchSubLines.ToDictionary(rsl => rsl.Id, rsl => rsl);
            var usersDict = users.ToDictionary(u => u.Id, u => u);

            // 8. Creamos la lista de resultados
            var result = new List<ProposalWithDetailsResponseDto>();

            foreach (var proposal in proposalsList)
            {
                // Filtramos los estudiantes solo para esta propuesta
                var proposalStudents = userInscriptionModalities
                    .Where(uim => uim.IdInscriptionModality == proposal.Id)
                    .ToList();

                // Preparamos los DTOs de estudiantes
                var studentsDto = _mapper.Map<List<UserInscriptionModalityDto>>(proposalStudents);
                
                // Asignamos eficientemente los nombres usando el diccionario de usuarios
                foreach (var student in studentsDto)
                {
                    if (usersDict.TryGetValue(student.IdUser, out var user))
                    {
                        student.UserName = $"{user.FirstName} {user.LastName}";
                    }
                }

                // Obtenemos los datos relacionados de los diccionarios
                stateProposalsDict.TryGetValue(proposal.IdStateProposal, out var stateProposal);
                researchLinesDict.TryGetValue(proposal.IdResearchLine, out var researchLine);
                researchSubLinesDict.TryGetValue(proposal.IdResearchSubLine, out var researchSubLine);

                // Añadimos la propuesta al resultado
                result.Add(new ProposalWithDetailsResponseDto
                {
                    Proposal = _mapper.Map<ProposalDto>(proposal),
                    StateProposalName = stateProposal?.Name ?? string.Empty,
                    ResearchLineName = researchLine?.Name ?? string.Empty,
                    ResearchSubLineName = researchSubLine?.Name ?? string.Empty,
                    Students = studentsDto
                });
            }

            return result;
        }
    }
}