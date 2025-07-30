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

        /// <summary>
        /// Procesa un evento de propuesta. 
        /// NOTA: Este método debe ser llamado desde un scope ya establecido (ej: UpdateEntityCommandHandler)
        /// </summary>
        public async Task ProcessProposalEventAsync(Proposal proposal, StateStageEnum stateStage, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📧 Procesando notificación para Proposal ID: {Id}", proposal.Id);

                var eventData = await BuildProposalEventDataAsync(proposal, stateStage, cancellationToken);
                var eventName = GetEventNameFromState(stateStage);
                var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                
                _logger.LogInformation("✅ Evento {EventName} encolado para Proposal ID: {Id}, JobId: {JobId}", eventName, proposal.Id, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando evento de propuesta para Proposal ID: {Id}", proposal.Id);
                throw; // Re-throw para que el caller maneje el error
            }
        }

        private string GetEventNameFromState(StateStageEnum stateStage)
        {
            return stateStage switch
            {
                StateStageEnum.PROP_RADICADA => "PROPOSAL_SUBMITTED",
                StateStageEnum.PROP_PERTINENTE => "PROPOSAL_APPROVED", 
                StateStageEnum.PROP_NO_PERTINENTE => "PROPOSAL_REJECTED",
                _ => stateStage.ToString()
            };
        }

        private async Task<Dictionary<string, object>> BuildProposalEventDataAsync(Proposal proposal, StateStageEnum stateStage, CancellationToken cancellationToken)
        {
            var eventData = new Dictionary<string, object>();
            try
            {
                // 🔧 EJECUTAR CONSULTAS SECUENCIALMENTE para evitar DbContext concurrency issues
                // DbContext no es thread-safe, no podemos usar Task.WhenAll con el mismo contexto
                
                var studentsCount = await GetStudentsCountAsync(proposal.Id);
                var studentData = await GetStudentDataAsync(proposal.Id);
                var researchLineName = await GetResearchLineNameAsync(proposal.IdResearchLine);
                var researchSubLineName = await GetResearchSubLineNameAsync(proposal.IdResearchSubLine);

                // Solo los campos requeridos por las plantillas
                eventData["ProposalTitle"] = proposal.Title;
                eventData["StudentsCount"] = studentsCount;
                eventData["StudentNames"] = studentData.Names;
                eventData["StudentEmails"] = studentData.Emails;
                eventData["ResearchLineName"] = researchLineName;
                eventData["ResearchSubLineName"] = researchSubLineName;
                eventData["GeneralObjective"] = proposal.GeneralObjective ?? "";
                eventData["SpecificObjectives"] = proposal.SpecificObjectives?.Any() == true 
                    ? string.Join("<br>", proposal.SpecificObjectives) 
                    : "";
                eventData["Observation"] = proposal.Observation ?? "Sin observaciones";

                // Campos específicos para diferentes tipos de rechazo/observaciones
                eventData["RejectionComments"] = proposal.Observation ?? "Revisa los requisitos y vuelve a enviar la propuesta.";

                // Fechas según evento
                var currentDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                switch (stateStage)
                {
                    case StateStageEnum.PROP_RADICADA:
                        eventData["SubmissionDate"] = currentDate;
                        break;
                    case StateStageEnum.PROP_PERTINENTE:
                        eventData["ApprovalDate"] = currentDate;
                        break;
                    case StateStageEnum.PROP_NO_PERTINENTE:
                        eventData["ReviewDate"] = currentDate;
                        break;
                    // case StateStageEnum.PROP_AJUSTES:
                    //     eventData["ReviewDate"] = currentDate;
                    //     break;
                    // Agregar más casos según sea necesario
                }

                _logger.LogInformation("✅ Event data generado para Proposal ID: {ProposalId} con {Count} placeholders", 
                    proposal.Id, eventData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo datos para evento de propuesta ID: {ProposalId}", proposal.Id);
            }
            return eventData;
        }

        /// <summary>
        /// Obtiene datos consolidados de estudiantes para una propuesta
        /// </summary>
        private async Task<(string Names, string Emails)> GetStudentDataAsync(int proposalId)
        {
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();

            // Buscar todos los usuarios asociados a esta inscripción que estén activos
            var userInscriptions = await userInscriptionRepo.GetAllAsync(
                ui => ui.IdInscriptionModality == proposalId && ui.StatusRegister == true);
            
            if (!userInscriptions.Any()) 
            {
                _logger.LogWarning("⚠️ No se encontraron usuarios activos para Proposal ID: {ProposalId}", proposalId);
                return ("", "");
            }
            
            var userIds = userInscriptions.Select(ui => ui.IdUser).ToList();
            var users = await userRepo.GetAllAsync(u => userIds.Contains(u.Id));
            
            var names = users.Select(u => $"{u.FirstName} {u.LastName}").ToList();
            var emails = users.Where(u => !string.IsNullOrEmpty(u.Email))
                              .Select(u => u.Email).ToList();
            
            _logger.LogInformation("🔍 Encontrados {Count} estudiantes para Proposal ID: {ProposalId}", names.Count, proposalId);
            
            return (string.Join(", ", names), string.Join(", ", emails));
        }

        /// <summary>
        /// Obtiene el conteo total de estudiantes para una propuesta
        /// </summary>
        private async Task<int> GetStudentsCountAsync(int proposalId)
        {
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var userInscriptions = await userInscriptionRepo.GetAllAsync(
                ui => ui.IdInscriptionModality == proposalId && ui.StatusRegister == true);
            return userInscriptions.Count();
        }

        /// <summary>
        /// Obtiene el nombre de línea de investigación
        /// </summary>
        private async Task<string> GetResearchLineNameAsync(int researchLineId)
        {
            var researchLineRepo = _unitOfWork.GetRepository<ResearchLine, int>();
            var researchLine = await researchLineRepo.GetByIdAsync(researchLineId);
            return researchLine?.Name ?? "No especificada";
        }

        /// <summary>
        /// Obtiene el nombre de sublínea de investigación
        /// </summary>
        private async Task<string> GetResearchSubLineNameAsync(int researchSubLineId)
        {
            var researchSubLineRepo = _unitOfWork.GetRepository<ResearchSubLine, int>();
            var researchSubLine = await researchSubLineRepo.GetByIdAsync(researchSubLineId);
            return researchSubLine?.Name ?? "No especificada";
        }
    }
}
