using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Builder específico para eventos de TeachingAssignment.
    /// Single Responsibility: Solo construye datos para eventos de asignaciones docentes.
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
                // Obtener la asignación docente
                var assignmentRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
                var assignment = await assignmentRepo.GetByIdAsync(assignmentId);
                
                if (assignment == null)
                    throw new ArgumentException($"TeachingAssignment with ID {assignmentId} not found");

                // Obtener datos relacionados
                var teacherRepo = _unitOfWork.GetRepository<User, int>();
                var typeAssignmentRepo = _unitOfWork.GetRepository<TypeTeachingAssignment, int>();
                var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();

                // Obtener el docente asignado
                var teacher = await teacherRepo.GetByIdAsync(assignment.IdTeacher);
                
                // Obtener el tipo de asignación
                var typeAssignment = await typeAssignmentRepo.GetByIdAsync(assignment.IdTypeTeachingAssignment);
                
                // Obtener la inscripción
                var inscription = await inscriptionRepo.GetByIdAsync(assignment.IdInscriptionModality);
                
                if (inscription == null)
                    throw new ArgumentException($"InscriptionModality with ID {assignment.IdInscriptionModality} not found");

                // Obtener modalidad y período académico
                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(inscription.IdAcademicPeriod);
                
                // Obtener la fase actual (StageModality)
                StageModality? currentStage = null;
                if (inscription.IdStageModality.HasValue)
                {
                    currentStage = await stageModalityRepo.GetByIdAsync(inscription.IdStageModality.Value);
                }

                // Obtener el título del proyecto según la fase
                string projectTitle = await GetProjectTitleAsync(inscription);

                // Obtener datos de estudiantes asociados
                var (studentNames, studentEmails, studentCount) = await _studentDataService.GetStudentDataByInscriptionAsync(assignment.IdInscriptionModality);

                // Construir diccionario de datos
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
                // Verificar si tiene propuesta
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetFirstOrDefaultAsync(p => p.InscriptionModality.Id == inscription.Id, CancellationToken.None);
                
                if (proposal != null && !string.IsNullOrEmpty(proposal.Title))
                {
                    return proposal.Title;
                }

                // Si no hay propuesta, verificar anteproyecto
                // NOTA: El ID de PreliminaryProject es el mismo que el ID de InscriptionModality
                var preliminaryRepo = _unitOfWork.GetRepository<PreliminaryProject, int>();
                var preliminary = await preliminaryRepo.GetByIdAsync(inscription.Id);
                
                if (preliminary != null)
                {
                    return $"Anteproyecto - Inscripción #{inscription.Id}";
                }

                // Si no hay anteproyecto, verificar proyecto final
                // NOTA: El ID de ProjectFinal es el mismo que el ID de InscriptionModality
                var projectRepo = _unitOfWork.GetRepository<ProjectFinal, int>();
                var project = await projectRepo.GetByIdAsync(inscription.Id);
                
                if (project != null)
                {
                    return $"Proyecto Final - Inscripción #{inscription.Id}";
                }

                // Si no tiene ningún proyecto específico, usar información de la modalidad
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                
                return $"Trabajo de {modality?.Name ?? "modalidad"} - Inscripción #{inscription.Id}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting project title for InscriptionModality ID: {InscriptionId}", inscription.Id);
                return $"Proyecto - Inscripción #{inscription.Id}";
            }
        }
    }
}
