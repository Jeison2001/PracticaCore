using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services.Notifications.Builders
{
    /// <summary>
    /// Construye datos de notificación para Proposal extrayendo: título, objetivos, estado,
    /// línea de investigación, estudiantes, fechas deSubmission/aprobación y observaciones.
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
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetByIdAsync(proposalId);

                if (proposal == null)
                    throw new ArgumentException($"Proposal with ID {proposalId} not found");

                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscriptionModality = await inscriptionModalityRepo
                    .GetFirstOrDefaultAsync(im => im.Proposal != null && im.Proposal.Id == proposalId, CancellationToken.None);

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

                var researchLine = await researchLineRepo.GetByIdAsync(proposal.IdResearchLine);
                var researchSubLine = await researchSubLineRepo.GetByIdAsync(proposal.IdResearchSubLine);
                var stateStage = await stateStageRepo.GetByIdAsync(proposal.IdStateStage);

                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByProposalAsync(proposalId);

                var eventData = new Dictionary<string, object>
                {
                    ["ProposalId"] = proposal.Id,
                    ["ProposalTitle"] = proposal.Title ?? string.Empty,
                    ["ProposalDescription"] = proposal.Description ?? proposal.GeneralObjective ?? string.Empty,
                    ["GeneralObjective"] = proposal.GeneralObjective ?? string.Empty,
                    ["SpecificObjectives"] = string.Join("; ", proposal.SpecificObjectives ?? new List<string>()),
                    ["ProposalStateStage"] = stateStage?.Name ?? string.Empty,
                    ["EventType"] = eventType,
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["AcademicPeriod"] = academicPeriod?.Code ?? string.Empty,
                    ["AcademicPeriodCode"] = academicPeriod?.Code ?? string.Empty,
                    ["ResearchLineName"] = researchLine?.Name ?? string.Empty,
                    ["ResearchSubLineName"] = researchSubLine?.Name ?? string.Empty,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentCount"] = studentCount,
                    ["StudentsCount"] = studentCount,
                    ["SubmissionDate"] = proposal.CreatedAt.ToString("dd/MM/yyyy"),
                    ["ApprovalDate"] = proposal.UpdatedAt?.ToString("dd/MM/yyyy") ?? string.Empty,
                    ["Observation"] = proposal.Observation ?? string.Empty,
                    ["RejectionComments"] = proposal.Observation ?? string.Empty,
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
