using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.Queries.Proposal;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Common.Handlers.Proposal
{
    public class GetProposalWithDetailsQueryHandler : IRequestHandler<GetProposalWithDetailsQuery, ProposalWithDetailsResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private static readonly int ProcessorCount = Math.Max(1, Environment.ProcessorCount / 2);

        public GetProposalWithDetailsQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ProposalWithDetailsResponseDto> Handle(GetProposalWithDetailsQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtenemos los repositorios necesarios
            var proposalRepo = _unitOfWork.GetRepository<Domain.Entities.Proposal, int>();
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var stateProposalRepo = _unitOfWork.GetRepository<StateProposal, int>();
            var researchLineRepo = _unitOfWork.GetRepository<ResearchLine, int>();
            var researchSubLineRepo = _unitOfWork.GetRepository<ResearchSubLine, int>();
            
            // 2. Obtenemos la propuesta por su ID
            var proposal = await proposalRepo.GetByIdAsync(request.Id);
            
            if (proposal == null)
            {
                throw new KeyNotFoundException($"No se encontró la propuesta con ID {request.Id}");
            }

            // 3. Obtenemos los datos relacionados secuencialmente para evitar problemas con el DbContext
            var stateProposal = await stateProposalRepo.GetByIdAsync(proposal.IdStateProposal);
            var researchLine = await researchLineRepo.GetByIdAsync(proposal.IdResearchLine);
            var researchSubLine = await researchSubLineRepo.GetByIdAsync(proposal.IdResearchSubLine);
            var students = (await userInscriptionRepo.GetAllAsync(
                filter: uim => uim.IdInscriptionModality == proposal.Id)).ToList();

            // 4. Optimización para la carga de datos de usuarios
            var userIds = students.Select(s => s.IdUser).Distinct().ToList();
            
            // Cargamos todos los usuarios en una sola consulta
            var users = (await userRepo.GetAllAsync(
                filter: u => userIds.Contains(u.Id))).ToList();
            
            // Creamos un diccionario para acceso rápido
            var userDict = users.ToDictionary(u => u.Id, u => u);
                
            // 5. Preparamos los DTOs de estudiantes de manera eficiente
            var studentsDto = _mapper.Map<List<UserInscriptionModalityDto>>(students);
            
            // Asignamos los nombres de usuarios utilizando el diccionario
            foreach (var student in studentsDto)
            {
                if (userDict.TryGetValue(student.IdUser, out var user))
                {
                    student.UserName = $"{user.FirstName} {user.LastName}";
                }
            }
            
            //// 6. Creamos la respuesta final
            //var modalityDto = proposal.InscriptionModality != null 
            //    ? _mapper.Map<InscriptionModalityDto>(proposal.InscriptionModality) 
            //    : new InscriptionModalityDto();
                
            var response = new ProposalWithDetailsResponseDto
            {
                Proposal = _mapper.Map<ProposalDto>(proposal),
                //InscriptionModality = modalityDto,
                StateProposalName = stateProposal?.Name ?? string.Empty,
                ResearchLineName = researchLine?.Name ?? string.Empty,
                ResearchSubLineName = researchSubLine?.Name ?? string.Empty,
                Students = studentsDto
            };

            return response;
        }
    }
}