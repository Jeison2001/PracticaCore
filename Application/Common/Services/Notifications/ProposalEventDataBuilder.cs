using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Builder específico para eventos de Proposal.
    /// Single Responsibility: Solo construye datos para eventos de Proposal.
    /// </summary>
    public class ProposalEventDataBuilder : IProposalEventDataBuilder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentDataService _studentDataService;
        private readonly ILogger<ProposalEventDataBuilder> _logger;

        public ProposalEventDataBuilder(
            IUnitOfWork unitOfWork,
            IStudentDataService studentDataService,
            ILogger<ProposalEventDataBuilder> logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildProposalEventDataAsync(int proposalId, string eventType)
        {
            try
            {
                // Obtener la propuesta
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetByIdAsync(proposalId);
                
                if (proposal == null)
                    throw new ArgumentException($"Proposal with ID {proposalId} not found");

                // Obtener la InscriptionModality asociada a la propuesta
                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscriptionModality = await inscriptionModalityRepo
                    .GetFirstOrDefaultAsync(im => im.Proposal != null && im.Proposal.Id == proposalId, CancellationToken.None);

                // Obtener datos relacionados
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var researchLineRepo = _unitOfWork.GetRepository<ResearchLine, int>();
                var researchSubLineRepo = _unitOfWork.GetRepository<ResearchSubLine, int>();
                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();

                Modality? modality = null;
                AcademicPeriod? academicPeriod = null;
                
                if (inscriptionModality != null)
                {
                    modality = await modalityRepo.GetByIdAsync(inscriptionModality.IdModality);
                    academicPeriod = await academicPeriodRepo.GetByIdAsync(inscriptionModality.IdAcademicPeriod);
                }

                // Obtener líneas de investigación
                var researchLine = await researchLineRepo.GetByIdAsync(proposal.IdResearchLine);
                var researchSubLine = await researchSubLineRepo.GetByIdAsync(proposal.IdResearchSubLine);
                var stateStage = await stateStageRepo.GetByIdAsync(proposal.IdStateStage);

                // Obtener datos de estudiantes asociados
                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByProposalAsync(proposalId);

                // Construir diccionario de datos
                var eventData = new Dictionary<string, object>
                {
                    ["ProposalId"] = proposal.Id,
                    ["ProposalTitle"] = proposal.Title ?? string.Empty,
                    ["ProposalDescription"] = proposal.GeneralObjective ?? string.Empty,
                    ["ProposalStateStage"] = stateStage?.Name ?? string.Empty,
                    ["EventType"] = eventType,
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["AcademicPeriodCode"] = academicPeriod?.Code ?? string.Empty,
                    ["ResearchLineName"] = researchLine?.Name ?? string.Empty,
                    ["ResearchSubLineName"] = researchSubLine?.Name ?? string.Empty,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentCount"] = studentCount,
                    ["CreatedAt"] = proposal.CreatedAt,
                    ["UpdatedAt"] = proposal.UpdatedAt ?? DateTime.UtcNow
                };

                _logger.LogDebug("Built event data for Proposal ID: {ProposalId}, Event: {EventType}", proposalId, eventType);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building event data for Proposal ID: {ProposalId}, Event: {EventType}", proposalId, eventType);
                throw;
            }
        }
    }
}
