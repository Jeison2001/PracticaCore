using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    /// <summary>
    /// Construye datos de notificación para TeachingAssignment extrayendo: docente,
    /// tipo de asignación, título del proyecto (desde Proposal o AcademicPractice según modalidad),
    /// estudiantes, fase actual y período académico.
    /// </summary>
    public class TeachingAssignmentEventDataBuilder : ITeachingAssignmentEventDataBuilder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentDataService _studentDataService;
        private readonly ILogger<TeachingAssignmentEventDataBuilder> _logger;

        public TeachingAssignmentEventDataBuilder(
            IUnitOfWork unitOfWork,
            IStudentDataService studentDataService,
            ILogger<TeachingAssignmentEventDataBuilder> logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildTeachingAssignmentEventDataAsync(int assignmentId, string eventType)
        {
            try
            {
                var assignmentRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
                var assignment = await assignmentRepo.GetByIdAsync(assignmentId);

                if (assignment == null)
                    throw new ArgumentException($"TeachingAssignment with ID {assignmentId} not found");

                var teacherRepo = _unitOfWork.GetRepository<User, int>();
                var typeAssignmentRepo = _unitOfWork.GetRepository<TypeTeachingAssignment, int>();
                var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();

                var teacher = await teacherRepo.GetByIdAsync(assignment.IdTeacher);

                var typeAssignment = await typeAssignmentRepo.GetByIdAsync(assignment.IdTypeTeachingAssignment);

                var inscription = await inscriptionRepo.GetByIdAsync(assignment.IdInscriptionModality);

                if (inscription == null)
                    throw new ArgumentException($"InscriptionModality with ID {assignment.IdInscriptionModality} not found");

                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(inscription.IdAcademicPeriod);

                StageModality? currentStage = null;
                if (inscription.IdStageModality.HasValue)
                {
                    currentStage = await stageModalityRepo.GetByIdAsync(inscription.IdStageModality.Value);
                }

                string projectTitle = await GetProjectTitleAsync(inscription);

                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByInscriptionAsync(assignment.IdInscriptionModality);

                var eventData = new Dictionary<string, object>
                {
                    ["AssignmentId"] = assignment.Id,
                    ["TeacherId"] = assignment.IdTeacher,
                    ["TeacherName"] = $"{teacher?.FirstName} {teacher?.LastName}".Trim(),
                    ["TeacherEmail"] = teacher?.Email ?? string.Empty,
                    ["AssignmentType"] = typeAssignment?.Name ?? string.Empty,
                    ["AssignmentTypeCode"] = typeAssignment?.Code ?? string.Empty,
                    ["ProjectTitle"] = projectTitle,
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["CurrentStage"] = currentStage?.Name ?? "Sin fase definida",
                    ["AcademicPeriod"] = academicPeriod?.Code ?? string.Empty,
                    ["AssignmentDate"] = assignment.CreatedAt.ToString("dd/MM/yyyy"),
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentsCount"] = studentCount,
                    ["EventType"] = eventType,
                    ["InscriptionId"] = inscription.Id,
                    ["CreatedAt"] = assignment.CreatedAt,
                    ["UpdatedAt"] = assignment.UpdatedAt ?? DateTime.UtcNow
                };

                _logger.LogDebug("Built event data for TeachingAssignment ID: {AssignmentId}, Event: {EventType}", assignmentId, eventType);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building event data for TeachingAssignment ID: {AssignmentId}, Event: {EventType}", assignmentId, eventType);
                throw;
            }
        }

        private async Task<string> GetProjectTitleAsync(InscriptionModality inscription)
        {
            try
            {
                var modalityRepoLocal = _unitOfWork.GetRepository<Modality, int>();
                var modalityLocal = await modalityRepoLocal.GetByIdAsync(inscription.IdModality);

                // PRACTICA_ACADEMICA: obtener título directamente de AcademicPractice
                if (modalityLocal?.Code == "PRACTICA_ACADEMICA")
                {
                    var practiceRepo = _unitOfWork.GetRepository<AcademicPractice, int>();
                    var practice = await practiceRepo.GetByIdAsync(inscription.Id);
                    if (practice != null && !string.IsNullOrWhiteSpace(practice.Title))
                    {
                        return practice.Title;
                    }
                }

                // Obtener título de la propuesta
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetFirstOrDefaultAsync(p => p.InscriptionModality.Id == inscription.Id, CancellationToken.None);

                if (proposal != null && !string.IsNullOrEmpty(proposal.Title))
                {
                    return proposal.Title;
                }

                // Sin propuesta o título: usar información de la modalidad
                var modalityFallback = modalityLocal ?? await modalityRepoLocal.GetByIdAsync(inscription.IdModality);
                return $"Trabajo de {modalityFallback?.Name ?? "modalidad"} - Inscripción #{inscription.Id}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting project title for InscriptionModality ID: {InscriptionId}", inscription.Id);
                return $"Proyecto - Inscripción #{inscription.Id}";
            }
        }
    }
}
