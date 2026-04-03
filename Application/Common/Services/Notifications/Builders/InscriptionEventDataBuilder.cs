using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    /// <summary>
    /// Builder específico para eventos de InscriptionModality.
    /// Single Responsibility: Solo construye datos para eventos de InscriptionModality.
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

                var eventData = new Dictionary<string, object>
                {
                    ["InscriptionId"] = inscription.Id,
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
                    ["InscriptionDate"] = inscription.CreatedAt.ToString("dd/MM/yyyy"),
                    ["ApprovalDate"] = inscription.ApprovalDate?.ToString("dd/MM/yyyy") ?? inscription.UpdatedAt?.ToString("dd/MM/yyyy") ?? string.Empty,
                    ["ReviewDate"] = inscription.UpdatedAt?.ToString("dd/MM/yyyy") ?? string.Empty,
                    ["ApprovalComments"] = inscription.Observations ?? string.Empty,
                    ["RejectionComments"] = inscription.Observations ?? string.Empty,
                    ["CreatedAt"] = inscription.CreatedAt,
                    ["UpdatedAt"] = inscription.UpdatedAt ?? DateTime.UtcNow,
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

                var modality = await modalityRepo.GetByIdAsync(modalityId);
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(academicPeriodId);

                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByUserIdsAsync(studentIds);

                var eventData = new Dictionary<string, object>
                {
                    ["InscriptionId"] = inscriptionId,
                    ["InscriptionState"] = "PENDIENTE",
                    ["EventType"] = "INSCRIPTION_CREATED",
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["AcademicPeriod"] = academicPeriod?.Code ?? string.Empty,
                    ["AcademicPeriodCode"] = academicPeriod?.Code ?? string.Empty,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentCount"] = studentCount,
                    ["StudentsCount"] = studentCount,
                    ["InscriptionDate"] = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                    ["ApprovalDate"] = string.Empty,
                    ["ReviewDate"] = string.Empty,
                    ["ApprovalComments"] = string.Empty,
                    ["RejectionComments"] = string.Empty,
                    ["CreatedAt"] = DateTime.UtcNow
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

        private string GetNextStepsByModality(string modalityCode)
        {
            return modalityCode switch
            {
                "PROYECTO_GRADO" => "<li>Proceder con la radicación de la propuesta</li><li>Mantente atento a futuras comunicaciones del comité académico</li>",
                "PRACTICA_ACADEMICA" => "<li>Proceder con la inscripción de la práctica académica</li><li>Completar el formulario de inscripción de práctica</li>",
                "CO_TERMINAL" => "<li>Gestionar los trámites de co-terminalidad con posgrado</li><li>Entregar documentación requerida</li>",
                "SEMINARIO_ACT" => "<li>Preparar y presentar el seminario de actualización</li><li>Obtener aprobación del comité</li>",
                "PUBLICACION_ARTICULO" => "<li>Gestionar la publicación del artículo científico</li><li>Verificar indexación en Publindex/Scimago/Scopus</li>",
                "GRADO_PROMEDIO" => "<li>Tramitar grado por promedio académico</li><li>Verificar cumplimiento de requisitos</li>",
                "SABER_PRO" => "<li>Gestionar grado según resultados Saber Pro</li><li>Entregar documentación requerida</li>",
                _ => "<li>Consultar con la coordinación los próximos pasos</li>"
            };
        }
    }
}
