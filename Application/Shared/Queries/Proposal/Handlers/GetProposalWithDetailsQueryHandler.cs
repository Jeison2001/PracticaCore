using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using AutoMapper;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.Proposal.Handlers
{
    public class GetProposalWithDetailsQueryHandler : IRequestHandler<GetProposalWithDetailsQuery, ProposalWithDetailsResponseDto>
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly IMapper _mapper;

        public GetProposalWithDetailsQueryHandler(
            IProposalRepository proposalRepository,
            IMapper mapper)
        {
            _proposalRepository = proposalRepository;
            _mapper = mapper;
        }

        public async Task<ProposalWithDetailsResponseDto> Handle(GetProposalWithDetailsQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtener la propuesta con todos sus detalles en una sola consulta usando el repositorio específico
            var proposalDetail = await _proposalRepository.GetProposalWithDetailsAsync(request.Id, cancellationToken);
            
            if (proposalDetail == null)
            {
                throw new KeyNotFoundException($"No se encontró la propuesta con ID {request.Id}");
            }

            // 2. Mapear la propuesta a DTO
            var proposalDto = _mapper.Map<ProposalDto>(proposalDetail.Proposal);
            
            // 3. Preparamos los DTOs de estudiantes
            var studentsDto = new List<UserInscriptionModalityDto>();
            
            // Creamos DTOs solo con los campos necesarios (idInscriptionModality, idUser, userName, email)
            foreach (var uim in proposalDetail.UserInscriptionModalities)
            {
                var studentDto = new UserInscriptionModalityDto
                {
                    IdInscriptionModality = uim.IdInscriptionModality,
                    IdUser = uim.IdUser,
                    UserName = uim.User?.FirstName + " " + uim.User?.LastName,
                    Email = uim.User?.Email ?? string.Empty
                };
                
                studentsDto.Add(studentDto);
            }
              // 4. Creamos la respuesta final
            var response = new ProposalWithDetailsResponseDto
            {
                Proposal = proposalDto,
                StateStageName = proposalDetail.Proposal.StateStage?.Name ?? string.Empty,
                StateStageCode = proposalDetail.Proposal.StateStage?.Code ?? string.Empty,
                ResearchLineName = proposalDetail.Proposal.ResearchLine?.Name ?? string.Empty,
                ResearchSubLineName = proposalDetail.Proposal.ResearchSubLine?.Name ?? string.Empty,
                Students = studentsDto
            };

            return response;
        }
    }
}