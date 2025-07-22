using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;
using Domain.Interfaces;

namespace Application.Common.Services
{
    /// <summary>
    /// Servicio para procesar notificaciones automáticas de eventos de propuesta
    /// </summary>
    public class ProposalNotificationService : IProposalNotificationService
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProposalNotificationService> _logger;

        public ProposalNotificationService(
            IEmailNotificationQueueService queueService,
            IUnitOfWork unitOfWork,
            ILogger<ProposalNotificationService> logger)
        {
            _queueService = queueService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ProcessProposalEventAsync(Proposal proposal, StateStageEnum stateStage, CancellationToken cancellationToken = default)
        {
            try
            {
                var eventData = await BuildProposalEventDataAsync(proposal, stateStage, cancellationToken);
                var eventName = stateStage.ToString();
                var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                _logger.LogInformation("Evento {EventName} encolado para Proposal ID: {Id}, JobId: {JobId}", eventName, proposal.Id, jobId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento de propuesta para Proposal ID: {Id}", proposal.Id);
            }
        }

        private async Task<Dictionary<string, object>> BuildProposalEventDataAsync(Proposal proposal, StateStageEnum stateStage, CancellationToken cancellationToken)
        {
            var eventData = new Dictionary<string, object>();
            try
            {
                // Solo los campos requeridos por las plantillas
                eventData["ProposalTitle"] = proposal.Title;
                eventData["StudentsCount"] = await GetStudentsCountAsync(proposal.Id);
                eventData["StudentNames"] = await GetStudentNamesAsync(proposal.Id);
                eventData["StudentEmails"] = await GetStudentEmailsAsync(proposal.Id);
                eventData["ResearchLineName"] = await GetResearchLineNameAsync(proposal.IdResearchLine);
                eventData["ResearchSubLineName"] = await GetResearchSubLineNameAsync(proposal.IdResearchSubLine);
                eventData["GeneralObjective"] = proposal.GeneralObjective ?? "";
                eventData["SpecificObjectives"] = proposal.SpecificObjectives != null ? string.Join("<br>", proposal.SpecificObjectives) : "";
                eventData["Observation"] = proposal.Observation ?? "";

                // Fechas según evento
                if (stateStage == StateStageEnum.PROP_RADICADA)
                    eventData["SubmissionDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                if (stateStage == StateStageEnum.PROP_PERTINENTE)
                    eventData["ApprovalDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                if (stateStage == StateStageEnum.PROP_NO_PERTINENTE)
                    eventData["ReviewDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                // Si se requiere lógica para ajustes, agregar aquí cuando el estado esté confirmado en la BD
                // Ejemplo:
                // if (stateStage == StateStageEnum.PROP_AJUSTES)
                //     eventData["ReviewDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo datos para evento de propuesta. Usando valores por defecto.");
                eventData["ProposalTitle"] = proposal.Title;
                eventData["StudentsCount"] = 1;
                eventData["StudentNames"] = "Estudiante";
                eventData["StudentEmails"] = "";
                eventData["ResearchLineName"] = "Línea";
                eventData["ResearchSubLineName"] = "Sub línea";
                eventData["GeneralObjective"] = "";
                eventData["SpecificObjectives"] = "";
                eventData["Observation"] = "";
            }
            return eventData;
        }

        private async Task<string> GetModalityNameAsync(int modalityId)
        {
            var repo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await repo.GetByIdAsync(modalityId);
            return modality?.Name ?? "Modalidad";
        }

        private async Task<string> GetAcademicPeriodCodeAsync(int periodId)
        {
            var repo = _unitOfWork.GetRepository<AcademicPeriod, int>();
            var period = await repo.GetByIdAsync(periodId);
            return period?.Code ?? "Período";
        }

        private async Task<int> GetStudentsCountAsync(int proposalId)
        {
            var repo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var users = await repo.GetAllAsync(ui => ui.IdInscriptionModality == proposalId && ui.StatusRegister == true);
            return users.Count();
        }

        private async Task<string> GetStudentNamesAsync(int proposalId)
        {
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var userInscriptions = await userInscriptionRepo.GetAllAsync(ui => ui.IdInscriptionModality == proposalId && ui.StatusRegister == true);
            var names = new List<string>();
            foreach (var ui in userInscriptions)
            {
                var user = await userRepo.GetByIdAsync(ui.IdUser);
                if (user != null)
                    names.Add($"{user.FirstName} {user.LastName}");
            }
            return string.Join(", ", names);
        }

        private async Task<string> GetStudentEmailsAsync(int proposalId)
        {
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var userInscriptions = await userInscriptionRepo.GetAllAsync(ui => ui.IdInscriptionModality == proposalId && ui.StatusRegister == true);
            var emails = new List<string>();
            foreach (var ui in userInscriptions)
            {
                var user = await userRepo.GetByIdAsync(ui.IdUser);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                    emails.Add(user.Email);
            }
            return string.Join(", ", emails);
        }

        private async Task<string> GetResearchLineNameAsync(int researchLineId)
        {
            var repo = _unitOfWork.GetRepository<ResearchLine, int>();
            var line = await repo.GetByIdAsync(researchLineId);
            return line?.Name ?? "Línea";
        }

        private async Task<string> GetResearchSubLineNameAsync(int researchSubLineId)
        {
            var repo = _unitOfWork.GetRepository<ResearchSubLine, int>();
            var subLine = await repo.GetByIdAsync(researchSubLineId);
            return subLine?.Name ?? "Sub línea";
        }
    }


    // Usar el enum de StateStage para reflejar los estados reales de la BD
    // El enum se encuentra en Application.Shared.DTOs.Enums.StateStageEnum
    // Si se requiere lógica para ajustes, agregar cuando el estado esté confirmado en la BD

    // public enum ProposalEventType
    // {
    //     PROPOSAL_SUBMITTED,
    //     PROPOSAL_APPROVED,
    //     PROPOSAL_REJECTED,
    //     PROPOSAL_ADJUSTMENTS
    // }

}
