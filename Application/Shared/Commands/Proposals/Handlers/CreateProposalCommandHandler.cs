using Application.Shared.DTOs.Proposals;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using Application.Common.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.Proposals.Handlers
{
    public class CreateProposalCommandHandler : IRequestHandler<CreateProposalCommand, ProposalDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<CreateProposalCommandHandler> _logger;

        public CreateProposalCommandHandler(
            IUnitOfWork unitOfWork, 
            IJobEnqueuer jobEnqueuer,
            ILogger<CreateProposalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<ProposalDto> Handle(CreateProposalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando creación de Proposal para InscriptionModality ID: {Id}", request.Dto.Id);

            // 1. Validar que existe la inscripción de modalidad
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            var inscription = await inscriptionModalityRepo.GetByIdAsync(request.Dto.Id);
            if (inscription == null)
                throw new InvalidOperationException($"No se encontró la inscripción de modalidad con ID {request.Dto.Id}");

            // 1b. Obtener MaxSpecificObjectives de la modalidad y validar límite de objetivos
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);

            if (modality?.MaxSpecificObjectives.HasValue == true &&
                request.Dto.SpecificObjectives.Count > modality.MaxSpecificObjectives.Value)
            {
                throw new InvalidOperationException(
                    $"El número de objetivos específicos ({request.Dto.SpecificObjectives.Count}) no puede exceder el máximo permitido ({modality.MaxSpecificObjectives.Value})");
            }

            // 2. Validar que la inscripción ya tiene fase propuesta (validación de negocio)
            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
            var propuestaStage = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.Code == StageModalityCodes.PgFasePropuesta && x.StatusRegister, cancellationToken);
            if (propuestaStage == null)
                throw new InvalidOperationException($"No se encontró la fase {StageModalityCodes.PgFasePropuesta} activa");

            if (inscription.IdStageModality != propuestaStage.Id)
                throw new InvalidOperationException($"La inscripción no está en fase {StageModalityCodes.PgFasePropuesta}. Estado actual: {inscription.IdStageModality}");

            // 3. Buscar el estado inicial para la fase PG_FASE_PROPUESTA (equivalente al trigger trg_set_initial_proposal_state)
            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var initialState = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.IdStageModality == propuestaStage.Id && x.IsInitialState && x.StatusRegister, cancellationToken);
            if (initialState == null)
                throw new InvalidOperationException($"No se encontró el estado inicial para la fase {StageModalityCodes.PgFasePropuesta}");

            // 4. Verificar que no existe ya una propuesta para esta inscripción
            var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
            var existingProposal = await proposalRepo.GetByIdAsync(request.Dto.Id);
            if (existingProposal != null)
                throw new InvalidOperationException($"Ya existe una propuesta para la inscripción ID {request.Dto.Id}");

            // 5. Validar que existen las líneas de investigación
            var researchLineRepo = _unitOfWork.GetRepository<ResearchLine, int>();
            var researchLine = await researchLineRepo.GetByIdAsync(request.Dto.IdResearchLine);
            if (researchLine == null)
                throw new InvalidOperationException($"No se encontró la línea de investigación con ID {request.Dto.IdResearchLine}");

            var researchSubLineRepo = _unitOfWork.GetRepository<ResearchSubLine, int>();
            var researchSubLine = await researchSubLineRepo.GetByIdAsync(request.Dto.IdResearchSubLine);
            if (researchSubLine == null)
                throw new InvalidOperationException($"No se encontró la sublínea de investigación con ID {request.Dto.IdResearchSubLine}");

            // 6. Crear la propuesta con el estado inicial (reemplaza el trigger trg_set_initial_proposal_state)
            var proposal = new Proposal
            {
                Id = request.Dto.Id,
                IdStateStage = initialState.Id,
                Title = request.Dto.Title,
                Description = request.Dto.Description,
                GeneralObjective = request.Dto.GeneralObjective,
                SpecificObjectives = request.Dto.SpecificObjectives,
                Observation = request.Dto.Observation,
                IdResearchLine = request.Dto.IdResearchLine,
                IdResearchSubLine = request.Dto.IdResearchSubLine,
                IdUserCreatedAt = request.CurrentUser?.UserId,
                OperationRegister = "Creada por CreateProposalCommand - Estado inicial asignado por dominio",
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow
            };

            await proposalRepo.AddAsync(proposal);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Proposal creada exitosamente con ID {Id} y estado inicial {StateStage}. Encolando notificación.",
                proposal.Id, initialState.Code);

            ProcessNotificationsAsync(proposal.Id);

            // 7. Retornar DTO
            return new ProposalDto
            {
                Id = proposal.Id,
                Title = proposal.Title,
                Description = proposal.Description,
                GeneralObjective = proposal.GeneralObjective,
                SpecificObjectives = proposal.SpecificObjectives,
                Observation = proposal.Observation,
                IdResearchLine = proposal.IdResearchLine,
                IdResearchSubLine = proposal.IdResearchSubLine,
                IdStateStage = proposal.IdStateStage,
                CreatedAt = proposal.CreatedAt
            };
        }

        private void ProcessNotificationsAsync(int proposalId)
        {
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x =>
                x.HandleProposalCreationAsync(proposalId));
        }
    }
}
