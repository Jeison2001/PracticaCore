using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    /// <summary>
    /// Lógica compartida para construir datos de evento de notificación para modalidades menores
    /// (CoTerminal, Seminar, SaberPro, ScientificArticle).
    /// </summary>
    public abstract class MinorModalityEventDataBuilderBase
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IStudentDataService _studentDataService;
        protected readonly ILogger _logger;

        protected MinorModalityEventDataBuilderBase(IUnitOfWork unitOfWork, IStudentDataService studentDataService, ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        protected async Task<Dictionary<string, object>> BuildBaseDataAsync(InscriptionModality inscription, int stateStageId, string eventType, string phaseLabel)
        {
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);

            var (studentNames, studentEmails, studentCount) =
                await _studentDataService.GetStudentDataByInscriptionAsync(inscription.Id);

            var stateInfo = await GetStateInfoAsync(stateStageId);
            var evaluationResult = GetEvaluationResultText(stateInfo.StateCode);

            return new Dictionary<string, object>
            {
                ["InscriptionModalityId"] = inscription.Id,
                ["StudentNames"] = studentNames,
                ["StudentEmails"] = studentEmails,
                ["StudentsCount"] = studentCount,
                ["ModalityName"] = modality?.Name ?? "Modalidad",
                ["CurrentState"] = stateInfo.StateName,
                ["CurrentStateCode"] = stateInfo.StateCode,
                ["EventType"] = eventType,
                ["Phase"] = phaseLabel,
                ["EvaluationResult"] = evaluationResult,
                ["SubmissionDate"] = DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                ["ApprovalDate"] = inscription.ApprovalDate?.ToString("dd/MM/yyyy") ?? string.Empty
            };
        }

        private async Task<(string StateName, string StateCode)> GetStateInfoAsync(int stateStageId)
        {
            var repo = _unitOfWork.GetRepository<StateStage, int>();
            var state = await repo.GetByIdAsync(stateStageId);
            return (state?.Name ?? "Estado desconocido", state?.Code ?? "UNKNOWN");
        }

        protected static string GetEvaluationResultText(string stateCode)
        {
            if (stateCode.Contains("APROBADO")) return "APROBADO";
            if (stateCode.Contains("OBSERVACIONES")) return "CON OBSERVACIONES";
            if (stateCode.Contains("RECHAZADO")) return "RECHAZADO";
            if (stateCode.Contains("RADICADO")) return "RADICADO";
            if (stateCode.Contains("PENDIENTE")) return "PENDIENTE";
            return "ESTADO DESCONOCIDO";
        }

        protected async Task<InscriptionModality?> GetInscriptionAsync(int entityId)
        {
            var repo = _unitOfWork.GetRepository<InscriptionModality, int>();
            return await repo.GetByIdAsync(entityId);
        }
    }
}
