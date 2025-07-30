using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
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
                // Obtener la inscripción
                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscription = await inscriptionModalityRepo.GetByIdAsync(inscriptionId);
                
                if (inscription == null)
                    throw new ArgumentException($"InscriptionModality with ID {inscriptionId} not found");

                // Obtener datos relacionados
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();

                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(inscription.IdAcademicPeriod);
                var stateInscription = await stateInscriptionRepo.GetByIdAsync(inscription.IdStateInscription);

                // Obtener datos de estudiantes asociados
                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByInscriptionAsync(inscriptionId);

                // Construir diccionario de datos
                var eventData = new Dictionary<string, object>
                {
                    ["InscriptionId"] = inscription.Id,
                    ["InscriptionState"] = stateInscription?.Name ?? string.Empty,
                    ["EventType"] = eventType,
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["AcademicPeriod"] = academicPeriod?.Code ?? string.Empty, // AcademicPeriod solo tiene Code
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
                    ["UpdatedAt"] = inscription.UpdatedAt ?? DateTime.UtcNow
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

        public async Task<Dictionary<string, object>> BuildBasicInscriptionDataAsync(int inscriptionId, int modalityId, int academicPeriodId, IEnumerable<int> studentIds)
        {
            try
            {
                // Versión simplificada para creación con datos mínimos disponibles
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();

                var modality = await modalityRepo.GetByIdAsync(modalityId);
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(academicPeriodId);

                // Obtener datos de estudiantes por IDs
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
                    ["StudentsCount"] = studentCount, // Alias para templates
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
    }
}
