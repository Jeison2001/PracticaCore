using Application.Common.Helpers;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    /// <summary>
    /// Construye datos de notificación para InscriptionModality extrayendo: estado,
    /// modalidad, período académico, estudiantes, fechas y comentarios de aprobación/rechazo.
    /// </summary>
    public class InscriptionEventDataBuilder : IInscriptionEventDataBuilder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentDataService _studentDataService;
        private readonly ILogger<InscriptionEventDataBuilder> _logger;

        public InscriptionEventDataBuilder(
            IUnitOfWork unitOfWork,
            IStudentDataService studentDataService,
            ILogger<InscriptionEventDataBuilder> logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildInscriptionEventDataAsync(int inscriptionId, string eventType)
        {
            try
            {
                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscription = await inscriptionModalityRepo.GetByIdAsync(inscriptionId);

                if (inscription == null)
                    throw new ArgumentException($"InscriptionModality with ID {inscriptionId} not found");

                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();

                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(inscription.IdAcademicPeriod);
                var stateInscription = await stateInscriptionRepo.GetByIdAsync(inscription.IdStateInscription);

                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByInscriptionAsync(inscriptionId);

                var tone = GetInscriptionTone(stateInscription?.Code, eventType);
                var cleanObservations = string.IsNullOrWhiteSpace(inscription.Observations)
                    ? string.Empty
                    : inscription.Observations.Trim();

                var eventData = new Dictionary<string, object>
                {
                    ["InscriptionId"] = inscription.Id,
                    ["InscriptionModalityId"] = inscription.Id,
                    ["InscriptionState"] = stateInscription?.Name ?? string.Empty,
                    ["EventType"] = eventType,
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["ModalityCode"] = modality?.Code ?? string.Empty,
                    ["AcademicPeriod"] = academicPeriod?.Code ?? string.Empty,
                    ["AcademicPeriodCode"] = academicPeriod?.Code ?? string.Empty,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentCount"] = studentCount,
                    ["StudentsCount"] = studentCount, // Alias para templates que usan StudentsCount
                    ["InscriptionDate"] = NotificationFormattingHelper.ToColombiaDate(inscription.CreatedAt),
                    ["ApprovalDate"] = NotificationFormattingHelper.ToColombiaDate(inscription.ApprovalDate ?? inscription.UpdatedAt),
                    ["ReviewDate"] = NotificationFormattingHelper.ToColombiaDate(inscription.UpdatedAt),
                    ["ApprovalComments"] = cleanObservations,
                    ["RejectionComments"] = cleanObservations,
                    ["TopAccentColor"] = NotificationFormattingHelper.GetTopAccentColor(tone),
                    ["ObservationsBlock"] = NotificationFormattingHelper.BuildObservationsBlockHtml(cleanObservations, tone),
                    ["CreatedAt"] = inscription.CreatedAt,
                    ["UpdatedAt"] = inscription.UpdatedAt ?? DateTimeOffset.UtcNow,
                    ["NextSteps"] = GetNextStepsByModality(modality?.Code ?? string.Empty)
                };

                _logger.LogDebug("Built event data for Inscription ID: {InscriptionId}, Event: {EventType}", inscriptionId, eventType);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building event data for Inscription ID: {InscriptionId}, Event: {EventType}", inscriptionId, eventType);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> BuildBasicInscriptionDataAsync(int inscriptionId, int modalityId, int academicPeriodId, IList<int> studentIds)
        {
            try
            {
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();

                var modality = await modalityRepo.GetByIdAsync(modalityId);
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(academicPeriodId);
                var inscription = inscriptionId > 0
                    ? await inscriptionModalityRepo.GetByIdAsync(inscriptionId)
                    : null;

                StateInscription? stateInscription = null;
                if (inscription?.IdStateInscription > 0)
                {
                    stateInscription = await stateInscriptionRepo.GetByIdAsync(inscription.IdStateInscription);
                }
                else if (modality != null)
                {
                    var expectedStateCode = modality.RequiresApproval
                        ? StateInscriptionCodes.Pendiente
                        : StateInscriptionCodes.NoAplica;
                    stateInscription = await stateInscriptionRepo.GetFirstOrDefaultAsync(
                        s => s.Code == expectedStateCode && s.StatusRegister,
                        CancellationToken.None);
                }

                var inscriptionState = !string.IsNullOrWhiteSpace(stateInscription?.Name)
                    ? stateInscription!.Name
                    : !string.IsNullOrWhiteSpace(stateInscription?.Code)
                        ? stateInscription!.Code
                        : modality?.RequiresApproval == false
                            ? StateInscriptionCodes.NoAplica
                            : StateInscriptionCodes.Pendiente;

                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByUserIdsAsync(studentIds);

                var createdAt = inscription != null && inscription.CreatedAt != default
                    ? inscription.CreatedAt
                    : DateTimeOffset.UtcNow;

                var eventData = new Dictionary<string, object>
                {
                    ["InscriptionId"] = inscriptionId,
                    ["InscriptionModalityId"] = inscriptionId,
                    ["InscriptionState"] = inscriptionState,
                    ["EventType"] = "INSCRIPTION_CREATED",
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["ModalityCode"] = modality?.Code ?? string.Empty,
                    ["AcademicPeriod"] = academicPeriod?.Code ?? string.Empty,
                    ["AcademicPeriodCode"] = academicPeriod?.Code ?? string.Empty,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentCount"] = studentCount,
                    ["StudentsCount"] = studentCount,
                    ["InscriptionDate"] = NotificationFormattingHelper.ToColombiaDate(createdAt),
                    ["ApprovalDate"] = string.Empty,
                    ["ReviewDate"] = string.Empty,
                    ["ApprovalComments"] = string.Empty,
                    ["RejectionComments"] = string.Empty,
                    ["TopAccentColor"] = NotificationFormattingHelper.GetTopAccentColor("info"),
                    ["ObservationsBlock"] = string.Empty,
                    ["CreatedAt"] = createdAt.UtcDateTime,
                    ["NextSteps"] = GetNextStepsByModality(modality?.Code ?? string.Empty)
                };

                _logger.LogDebug("Built basic inscription data for ID: {InscriptionId} with {StudentCount} students", inscriptionId, studentCount);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building basic inscription data for ID: {InscriptionId}", inscriptionId);
                throw;
            }
        }

        private static string GetInscriptionTone(string? stateCode, string? eventType)
        {
            var upperState = (stateCode ?? string.Empty).ToUpperInvariant();
            var upperEvent = (eventType ?? string.Empty).ToUpperInvariant();

            if (upperState == StateInscriptionCodes.Aprobado || upperEvent == "INSCRIPTION_APPROVED")
                return "approved";
            if (upperState == StateInscriptionCodes.Rechazado || upperEvent == "INSCRIPTION_REJECTED")
                return "rejected";
            return "info";
        }

        private static readonly Dictionary<string, string> ModalityNextStepsMap = new(StringComparer.OrdinalIgnoreCase)
        {
            {ModalityCodes.ProyectoGrado, "<li>Proceder con la radicación de la propuesta</li><li>Manténgase atento a futuras comunicaciones del comité académico</li>"},
            {ModalityCodes.PracticaAcademica, "<li>Proceder con la inscripción de la práctica académica</li><li>Completar el formulario de inscripción de práctica</li>"},
            {ModalityCodes.CoTerminal, "<li>Gestionar los trámites de co-terminalidad con posgrado</li><li>Entregar documentación requerida</li>"},
            {ModalityCodes.SeminarioAct, "<li>Preparar y presentar el seminario de actualización</li><li>Obtener aprobación del comité</li>"},
            {ModalityCodes.PublicacionArticulo, "<li>Gestionar la publicación del artículo científico</li><li>Verificar indexación en Publindex/Scimago/Scopus</li>"},
            {ModalityCodes.GradoPromedio, "<li>Tramitar grado por promedio académico</li><li>Verificar cumplimiento de requisitos</li>"},
            {ModalityCodes.SaberPro, "<li>Gestionar grado según resultados Saber Pro</li><li>Entregar documentación requerida</li>"}
        };

        private string GetNextStepsByModality(string modalityCode)
        {
            return ModalityNextStepsMap.TryGetValue(modalityCode, out var steps) 
                ? steps 
                : "<li>Consultar con la coordinación los próximos pasos</li>";
        }
    }
}
