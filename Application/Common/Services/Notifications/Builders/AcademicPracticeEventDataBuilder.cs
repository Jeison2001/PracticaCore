using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    /// <summary>
    /// Construye datos para eventos de notificaciones de Práctica Académica
    /// </summary>
    public class AcademicPracticeEventDataBuilder : IAcademicPracticeEventDataBuilder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentDataService _studentDataService;
        private readonly ILogger<AcademicPracticeEventDataBuilder> _logger;

        public AcademicPracticeEventDataBuilder(
            IUnitOfWork unitOfWork,
            IStudentDataService studentDataService,
            ILogger<AcademicPracticeEventDataBuilder> logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildAcademicPracticeEventDataAsync(int academicPracticeId, string eventType)
        {
            _logger.LogInformation("Building academic practice event data for AcademicPractice ID: {AcademicPracticeId}, Event: {EventType}",
                academicPracticeId, eventType);

            try
            {
                var apRepo = _unitOfWork.GetRepository<AcademicPractice, int>();
                var academicPractice = await apRepo.GetByIdAsync(academicPracticeId);
                if (academicPractice == null)
                {
                    _logger.LogWarning("AcademicPractice not found with ID: {AcademicPracticeId}", academicPracticeId);
                    return new Dictionary<string, object>();
                }

                // AcademicPractice.Id == InscriptionModality.Id (convención del proyecto)
                var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscription = await inscriptionRepo.GetByIdAsync(academicPracticeId);
                if (inscription == null)
                {
                    _logger.LogWarning("InscriptionModality not found with ID: {InscriptionModalityId}", academicPracticeId);
                    return new Dictionary<string, object>();
                }

                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                if (modality?.Code != "PRACTICA_ACADEMICA")
                {
                    _logger.LogInformation("AcademicPractice modality is not PRACTICA_ACADEMICA (Modality: {Modality}). Skipping.", modality?.Code);
                    return new Dictionary<string, object>();
                }

                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByInscriptionAsync(inscription.Id);

                var projectTitle = await GetProjectTitleAsync(inscription, academicPractice);

                var stateInfo = await GetStateInfoAsync(academicPractice.IdStateStage);

                var phaseInfo = GetPhaseInfo(stateInfo.StateCode);

                var submissionDate = academicPractice.UpdatedAt?.ToString("dd/MM/yyyy HH:mm") ?? academicPractice.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                var approvalDate = GetApprovalDate(stateInfo.StateCode, academicPractice);

                var eventData = new Dictionary<string, object>
                {
                    ["InscriptionModalityId"] = inscription.Id,
                    ["ProjectTitle"] = projectTitle,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentsCount"] = studentCount,
                    ["CurrentState"] = stateInfo.StateName,
                    ["CurrentStateCode"] = stateInfo.StateCode,
                    ["SubmissionDate"] = submissionDate,
                    ["EventType"] = eventType,
                    ["ApprovalDate"] = approvalDate,
                    ["Observations"] = academicPractice.Observations ?? string.Empty,

                    ["Phase"] = phaseInfo.Phase,
                    ["PhaseDescription"] = phaseInfo.PhaseDescription,
                    ["NextSteps"] = phaseInfo.NextSteps,
                    ["ObservationAction"] = phaseInfo.ObservationAction,
                    ["ImportantNote"] = phaseInfo.ImportantNote,
                    ["CongratulationMessage"] = phaseInfo.CongratulationMessage,
                    ["ReasonDescription"] = phaseInfo.ReasonDescription,
                    ["PossibleCausesTitle"] = phaseInfo.PossibleCausesTitle,
                    ["PossibleCauses"] = phaseInfo.PossibleCauses,
                    ["AvailableOptions"] = phaseInfo.AvailableOptions,
                    ["ImportantMessage"] = phaseInfo.ImportantMessage,
                    ["AdministrativeNextSteps"] = phaseInfo.AdministrativeNextSteps,

                    // BD-specific observations for OBSERVACIONES templates
                    ["SpecificObservations"] = academicPractice.Observations ?? string.Empty,
                    ["EvaluatorObservations"] = academicPractice.EvaluatorObservations ?? string.Empty
                };

                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building academic practice event data for ID: {AcademicPracticeId}", academicPracticeId);
                return new Dictionary<string, object>();
            }
        }

        private async Task<string> GetProjectTitleAsync(InscriptionModality inscription, AcademicPractice academicPractice)
        {
            try
            {
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetFirstOrDefaultAsync(p => p.Id == inscription.Id && p.StatusRegister, CancellationToken.None);
                return proposal?.Title ?? academicPractice.Title ?? "Práctica sin título";
            }
            catch
            {
                return academicPractice.Title ?? "Práctica sin título";
            }
        }

        private async Task<(string StateName, string StateCode)> GetStateInfoAsync(int stateStageId)
        {
            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var stateStage = await stateStageRepo.GetByIdAsync(stateStageId);
            return (stateStage?.Name ?? "Estado desconocido", stateStage?.Code ?? "UNKNOWN");
        }

        private PhaseInfo GetPhaseInfo(string stateCode)
        {
            if (!Enum.TryParse<StateStageCodeEnum>(stateCode, out var code))
                return new PhaseInfo();

            return code switch
            {
                // FASE INSCRIPCIÓN
                StateStageCodeEnum.PA_INSCRIPCION_EN_REVISION => new PhaseInfo
                {
                    Phase = "Inscripción",
                    PhaseDescription = "su solicitud de inscripción para la modalidad de Práctica Académica",
                    NextSteps = "<li>El comité revisará su documentación y le informará sobre la decisión en un plazo máximo de 5 días hábiles</li><li>Revisar el estado de su solicitud en el sistema</li><li>Mantener disponibilidad para posibles consultas</li>",
                    ObservationAction = "Debe revisar y corregir la documentación según las observaciones del comité. Una vez realizadas las correcciones, radique nuevamente los documentos.",
                    ImportantNote = "Esta revisión determinará si puede proceder a la fase de desarrollo de su práctica académica.",
                    CongratulationMessage = "Puede proceder inmediatamente a la fase de desarrollo de su práctica académica.",
                    ReasonDescription = "La documentación presentada no cumple con los requisitos establecidos para esta modalidad de grado.",
                    PossibleCausesTitle = "Motivos posibles de no aprobación:",
                    PossibleCauses = "<li>Documentación incompleta o incorrecta</li><li>Aval institucional no válido</li><li>Plan de trabajo insuficiente</li>",
                    AvailableOptions = "<li>Contactar con la coordinación para conocer los motivos específicos</li><li>Corregir y reradicar la documentación</li><li>Considerar otras modalidades de grado disponibles</li>",
                    ImportantMessage = "Puede corregir los aspectos señalados y volver a radicar su documentación."
                },
                StateStageCodeEnum.PA_INSCRIPCION_OBSERVACIONES => new PhaseInfo
                {
                    Phase = "Inscripción",
                    PhaseDescription = "su solicitud de inscripción para la modalidad de Práctica Académica",
                    NextSteps = "<li>Revisar las observaciones específicas comunicadas por el comité</li><li>Corregir la documentación según lo solicitado</li><li>Radicar nuevamente los documentos corregidos</li>",
                    ObservationAction = "Debe revisar y corregir la documentación según las observaciones del comité. Una vez realizadas las correcciones, radique nuevamente los documentos."
                },
                StateStageCodeEnum.PA_INSCRIPCION_APROBADA => new PhaseInfo
                {
                    Phase = "Inscripción",
                    PhaseDescription = "su solicitud de inscripción para la modalidad de Práctica Académica",
                    NextSteps = "<li>Ya puede proceder a la fase de desarrollo de su práctica académica</li><li>Debe cumplir con el mínimo de 640 horas de práctica</li><li>Realice seguimiento periódico según el cronograma establecido</li>",
                    ImportantNote = "Esta aprobación le habilita para iniciar inmediatamente la fase de desarrollo de su práctica académica en la institución aprobada.",
                    CongratulationMessage = "Puede proceder inmediatamente a la fase de desarrollo de su práctica académica."
                },
                StateStageCodeEnum.PA_INSCRIPCION_RECHAZADA => new PhaseInfo
                {
                    Phase = "Inscripción",
                    PhaseDescription = "su solicitud de inscripción para la modalidad de Práctica Académica",
                    ReasonDescription = "La documentación presentada no cumple con los requisitos establecidos para esta modalidad de grado.",
                    PossibleCausesTitle = "Motivos posibles de rechazo:",
                    PossibleCauses = "<li>Documentación incompleta o incorrecta</li><li>Aval institucional no válido</li><li>Plan de trabajo insuficiente</li><li>No cumplimiento de requisitos académicos</li>",
                    AvailableOptions = "<li>Contactar con la coordinación para conocer los motivos específicos</li><li>Considerar otras modalidades de grado disponibles</li><li>Revisar requisitos para una nueva propuesta</li>",
                    ImportantMessage = "Este resultado no define su capacidad académica. Le animamos a explorar otras opciones disponibles."
                },

                // FASE DESARROLLO
                StateStageCodeEnum.PA_DESARROLLO_EN_REVISION => new PhaseInfo
                {
                    Phase = "Desarrollo",
                    PhaseDescription = "su fase de desarrollo de práctica académica",
                    NextSteps = "<li>El comité revisará su documentación de desarrollo y le informará sobre la decisión en un plazo máximo de 15 días hábiles</li><li>Aguarde respuesta del comité evaluador</li><li>Mantenga disponibilidad para posibles consultas</li><li>Esté preparado para atender observaciones si las hay</li>",
                    ImportantNote = "Su documentación de desarrollo está en proceso de evaluación por parte del comité. Pronto recibirá respuesta del comité evaluador."
                },
                StateStageCodeEnum.PA_DESARROLLO_OBSERVACIONES => new PhaseInfo
                {
                    Phase = "Desarrollo",
                    PhaseDescription = "su fase de desarrollo de práctica académica",
                    NextSteps = "<li>Revisar las observaciones específicas del comité</li><li>Realizar las correcciones o ajustes solicitados</li><li>Mejorar aspectos señalados en el desarrollo</li><li>Mantener comunicación con su supervisor</li>",
                    ObservationAction = "Debe atender las observaciones del comité sobre su desarrollo de práctica académica y realizar las correcciones o ajustes solicitados."
                    // NO incluimos PossibleCauses porque las observaciones específicas están en academicPractice.Observations
                },
                StateStageCodeEnum.PA_DESARROLLO_APROBADA => new PhaseInfo
                {
                    Phase = "Desarrollo",
                    PhaseDescription = "la fase de desarrollo de su práctica académica",
                    NextSteps = "<li>Ya puede proceder a preparar su informe final de práctica</li><li>Solicite la certificación de la práctica a la institución</li><li>Prepare toda la documentación requerida para la evaluación final</li>",
                    ImportantNote = "Documentos a preparar: Informe final de práctica académica, Carta de certificación de la institución receptora, Formatos de seguimiento finales",
                    CongratulationMessage = "Ha completado exitosamente la fase de desarrollo! Ahora puede proceder con la fase final del proceso."
                },
                StateStageCodeEnum.PA_DESARROLLO_NO_APROBADA => new PhaseInfo
                {
                    Phase = "Desarrollo",
                    PhaseDescription = "su fase de desarrollo de práctica académica",
                    ReasonDescription = "La fase de desarrollo no alcanzó los estándares mínimos requeridos para continuar con el proceso.",
                    PossibleCausesTitle = "Posibles causas de no aprobación:",
                    PossibleCauses = "<li>Incumplimiento del número mínimo de horas de práctica (640)</li><li>Informes de seguimiento deficientes o incompletos</li><li>Evaluación negativa por parte de la institución receptora</li><li>No cumplimiento de los objetivos establecidos en el plan de trabajo</li>",
                    AvailableOptions = "<li>Solicitar reunión con el comité para conocer detalles específicos</li><li>Considerar la posibilidad de presentar una nueva propuesta</li><li>Explorar otras modalidades de grado disponibles</li>",
                    ImportantMessage = "Este resultado no define su capacidad académica. Le recomendamos contactar con la coordinación para recibir orientación."
                },

                // FASE INFORME FINAL
                StateStageCodeEnum.PA_INFORME_FINAL_EN_REVISION => new PhaseInfo
                {
                    Phase = "Informe Final",
                    PhaseDescription = "su informe final de práctica académica",
                    NextSteps = "<li>Los evaluadores tienen un plazo máximo de 25 días hábiles para completar la revisión de su informe final</li><li>Mantener comunicación con la coordinación si es necesario</li><li>El proceso incluye revisión técnica, evaluación de objetivos y verificación de certificación institucional</li>",
                    ImportantNote = "El proceso de evaluación incluye revisión técnica del informe, evaluación del cumplimiento de objetivos y verificación de la certificación institucional."
                },
                StateStageCodeEnum.PA_INFORME_FINAL_OBSERVACIONES => new PhaseInfo
                {
                    Phase = "Informe Final",
                    PhaseDescription = "su informe final de práctica académica",
                    NextSteps = "<li>Revisar detalladamente las observaciones específicas de los evaluadores</li><li>Corregir el informe final según lo solicitado</li><li>Radicar nuevamente el informe corregido</li><li>Contactar con su director de práctica si requiere orientación</li>",
                    ObservationAction = "Debe revisar y corregir el informe final según las observaciones de los evaluadores. Una vez realizadas las correcciones, radique nuevamente el documento.",
                    ImportantMessage = "Las correcciones son una oportunidad para mejorar la calidad de su trabajo. Una vez corregido, el documento será evaluado nuevamente."
                },

                // ESTADOS FINALES
                StateStageCodeEnum.PA_APROBADO => new PhaseInfo
                {
                    Phase = "Final",
                    PhaseDescription = "su modalidad de grado de Práctica Académica",
                    AdministrativeNextSteps = "Puede proceder con los trámites de grado. La coordinación del programa le informará sobre los procedimientos para la ceremonia de graduación y expedición del título."
                },
                StateStageCodeEnum.PA_NO_APROBADO => new PhaseInfo
                {
                    Phase = "Final",
                    PhaseDescription = "su práctica académica",
                    ReasonDescription = "La evaluación integral del proceso no alcanzó los estándares mínimos requeridos para esta modalidad de grado.",
                    AvailableOptions = "<li>Solicitar una reunión con el comité para conocer los detalles específicos</li><li>Considerar la posibilidad de presentar una nueva propuesta</li><li>Explorar otras modalidades de grado disponibles</li><li>Solicitar orientación académica para definir la mejor ruta hacia su titulación</li>",
                    ImportantMessage = "Este resultado no define su capacidad académica. Le animamos a explorar las opciones disponibles y a contactar con la coordinación para recibir orientación personalizada."
                },

                _ => new PhaseInfo()
            };
        }

        private class PhaseInfo
        {
            public string Phase { get; set; } = string.Empty;
            public string PhaseDescription { get; set; } = string.Empty;
            public string NextSteps { get; set; } = string.Empty;
            public string ObservationAction { get; set; } = string.Empty;
            public string ImportantNote { get; set; } = string.Empty;
            public string CongratulationMessage { get; set; } = string.Empty;
            public string ReasonDescription { get; set; } = string.Empty;
            public string PossibleCausesTitle { get; set; } = string.Empty;
            public string PossibleCauses { get; set; } = string.Empty;
            public string AvailableOptions { get; set; } = string.Empty;
            public string ImportantMessage { get; set; } = string.Empty;
            public string AdministrativeNextSteps { get; set; } = string.Empty;
        }

        private string GetApprovalDate(string stateCode, AcademicPractice ap)
        {
            if (!Enum.TryParse<StateStageCodeEnum>(stateCode, out var code)) return string.Empty;
            DateTimeOffset? date = code switch
            {
                // Fechas por fase
                StateStageCodeEnum.PA_INSCRIPCION_APROBADA => ap.PlanApprovalDate ?? ap.AvalApprovalDate,
                StateStageCodeEnum.PA_DESARROLLO_EN_REVISION => ap.DevelopmentCompletionDate,
                StateStageCodeEnum.PA_DESARROLLO_OBSERVACIONES => ap.DevelopmentCompletionDate,
                StateStageCodeEnum.PA_DESARROLLO_APROBADA => ap.DevelopmentCompletionDate,
                StateStageCodeEnum.PA_DESARROLLO_NO_APROBADA => ap.DevelopmentCompletionDate,
                StateStageCodeEnum.PA_APROBADO => ap.FinalApprovalDate ?? ap.FinalReportApprovalDate,
                StateStageCodeEnum.PA_NO_APROBADO => ap.FinalApprovalDate ?? ap.FinalReportApprovalDate,
                
                // Para estados en revisión, usar fechas base disponibles o CreatedAt
                StateStageCodeEnum.PA_INSCRIPCION_EN_REVISION => ap.CreatedAt,
                StateStageCodeEnum.PA_INFORME_FINAL_EN_REVISION => ap.PracticeEndDate,
                
                _ => null
            };
            return date?.ToString("dd/MM/yyyy") ?? string.Empty;
        }
    }
}
