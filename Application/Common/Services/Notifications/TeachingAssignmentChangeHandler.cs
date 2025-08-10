using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Handler específico para cambios en TeachingAssignment.
    /// Single Responsibility: Solo maneja notificaciones de asignaciones docentes.
    /// </summary>
    public class TeachingAssignmentChangeHandler : IEntityChangeHandler<TeachingAssignment, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly ITeachingAssignmentEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TeachingAssignmentChangeHandler> _logger;

        public TeachingAssignmentChangeHandler(
            IEmailNotificationQueueService queueService,
            ITeachingAssignmentEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<TeachingAssignmentChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(TeachingAssignment oldEntity, TeachingAssignment newEntity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para cambio en TeachingAssignment ID: {AssignmentId}", newEntity.Id);

            // Si cambió el docente asignado
            if (oldEntity.IdTeacher != newEntity.IdTeacher)
            {
                // Notificar al nuevo docente asignado
                var newTeacherEventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(newEntity.Id, "TEACHING_ASSIGNMENT_ASSIGNED");
                var newTeacherJobId = _queueService.EnqueueEventNotification("TEACHING_ASSIGNMENT_ASSIGNED", newTeacherEventData);
                _logger.LogInformation("📧 Evento TEACHING_ASSIGNMENT_ASSIGNED encolado para nuevo docente. ID: {AssignmentId}, JobId: {JobId}", 
                    newEntity.Id, newTeacherJobId);

                // Notificar a los estudiantes sobre el nuevo docente
                var studentsEventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(newEntity.Id, "STUDENT_TEACHER_ASSIGNED");
                var studentsJobId = _queueService.EnqueueEventNotification("STUDENT_TEACHER_ASSIGNED", studentsEventData);
                _logger.LogInformation("📧 Evento STUDENT_TEACHER_ASSIGNED encolado para estudiantes. ID: {AssignmentId}, JobId: {JobId}", 
                    newEntity.Id, studentsJobId);

                // Notificar al docente anterior (desasignado)
                if (oldEntity.IdTeacher > 0) // Verificar que había un docente previo
                {
                    var oldTeacherEventData = await BuildUnassignedTeacherEventDataAsync(oldEntity, newEntity);
                    var oldTeacherJobId = _queueService.EnqueueEventNotification("TEACHER_UNASSIGNED", oldTeacherEventData);
                    _logger.LogInformation("📧 Evento TEACHER_UNASSIGNED encolado para docente anterior. ID: {AssignmentId}, JobId: {JobId}", 
                        newEntity.Id, oldTeacherJobId);
                }
            }
            // Si solo cambió el tipo de asignación (mismo docente)
            else if (oldEntity.IdTypeTeachingAssignment != newEntity.IdTypeTeachingAssignment)
            {
                // Notificar al docente sobre el cambio de tipo
                var eventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(newEntity.Id, "TEACHING_ASSIGNMENT_ASSIGNED");
                var jobId = _queueService.EnqueueEventNotification("TEACHING_ASSIGNMENT_ASSIGNED", eventData);
                _logger.LogInformation("📧 Evento TEACHING_ASSIGNMENT_ASSIGNED encolado por cambio de tipo. ID: {AssignmentId}, JobId: {JobId}", 
                    newEntity.Id, jobId);

                // Notificar a los estudiantes sobre el cambio
                var studentsEventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(newEntity.Id, "STUDENT_TEACHER_ASSIGNED");
                var studentsJobId = _queueService.EnqueueEventNotification("STUDENT_TEACHER_ASSIGNED", studentsEventData);
                _logger.LogInformation("📧 Evento STUDENT_TEACHER_ASSIGNED encolado por cambio de tipo. ID: {AssignmentId}, JobId: {JobId}", 
                    newEntity.Id, studentsJobId);
            }

            _logger.LogInformation("✅ Procesamiento de notificaciones completado para TeachingAssignment ID: {AssignmentId}", newEntity.Id);
        }

        public async Task HandleCreationAsync(TeachingAssignment entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para nueva TeachingAssignment ID: {AssignmentId}", entity.Id);

            // Para nuevas asignaciones, notificar al docente asignado
            var teacherEventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(entity.Id, "TEACHING_ASSIGNMENT_ASSIGNED");
            var teacherJobId = _queueService.EnqueueEventNotification("TEACHING_ASSIGNMENT_ASSIGNED", teacherEventData);
            _logger.LogInformation("📧 Evento TEACHING_ASSIGNMENT_ASSIGNED encolado para nuevo docente. ID: {AssignmentId}, JobId: {JobId}", 
                entity.Id, teacherJobId);

            // Notificar a los estudiantes sobre la nueva asignación
            var studentsEventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(entity.Id, "STUDENT_TEACHER_ASSIGNED");
            var studentsJobId = _queueService.EnqueueEventNotification("STUDENT_TEACHER_ASSIGNED", studentsEventData);
            _logger.LogInformation("📧 Evento STUDENT_TEACHER_ASSIGNED encolado para estudiantes. ID: {AssignmentId}, JobId: {JobId}", 
                entity.Id, studentsJobId);

            _logger.LogInformation("✅ Procesamiento de notificaciones completado para nueva TeachingAssignment ID: {AssignmentId}", entity.Id);
        }

        /// <summary>
        /// Construye los datos del evento para el docente que fue desasignado
        /// </summary>
        private async Task<Dictionary<string, object>> BuildUnassignedTeacherEventDataAsync(TeachingAssignment oldEntity, TeachingAssignment newEntity)
        {
            try
            {
                // Obtener los datos del docente anterior
                var teacherRepo = _unitOfWork.GetRepository<User, int>();
                var oldTeacher = await teacherRepo.GetByIdAsync(oldEntity.IdTeacher);
                
                var typeAssignmentRepo = _unitOfWork.GetRepository<TypeTeachingAssignment, int>();
                var oldTypeAssignment = await typeAssignmentRepo.GetByIdAsync(oldEntity.IdTypeTeachingAssignment);
                
                var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscription = await inscriptionRepo.GetByIdAsync(oldEntity.IdInscriptionModality);
                
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscription?.IdModality ?? 0);
                
                var academicPeriodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var academicPeriod = await academicPeriodRepo.GetByIdAsync(inscription?.IdAcademicPeriod ?? 0);
                
                var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
                StageModality? currentStage = null;
                if (inscription?.IdStageModality.HasValue == true)
                {
                    currentStage = await stageModalityRepo.GetByIdAsync(inscription.IdStageModality.Value);
                }

                // Obtener el título del proyecto
                string projectTitle = await GetProjectTitleAsync(inscription);

                return new Dictionary<string, object>
                {
                    ["AssignmentId"] = oldEntity.Id,
                    ["TeacherId"] = oldEntity.IdTeacher,
                    ["TeacherName"] = $"{oldTeacher?.FirstName} {oldTeacher?.LastName}".Trim(),
                    ["UnassignedTeacherEmail"] = oldTeacher?.Email ?? string.Empty, // Campo específico para desasignación
                    ["TeacherEmail"] = oldTeacher?.Email ?? string.Empty, // Fallback
                    ["AssignmentType"] = oldTypeAssignment?.Name ?? string.Empty,
                    ["ProjectTitle"] = projectTitle,
                    ["ModalityName"] = modality?.Name ?? string.Empty,
                    ["CurrentStage"] = currentStage?.Name ?? "Sin fase definida",
                    ["AcademicPeriod"] = academicPeriod?.Code ?? string.Empty,
                    ["UnassignmentDate"] = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                    ["EventType"] = "TEACHER_UNASSIGNED",
                    ["InscriptionId"] = inscription?.Id ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building unassigned teacher event data for TeachingAssignment ID: {AssignmentId}", oldEntity.Id);
                throw;
            }
        }

        /// <summary>
        /// Obtiene el título del proyecto basado en la inscripción
        /// </summary>
        private async Task<string> GetProjectTitleAsync(InscriptionModality? inscription)
        {
            if (inscription == null) return "Proyecto no identificado";

            try
            {
                // Verificar si tiene propuesta
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetFirstOrDefaultAsync(p => p.InscriptionModality.Id == inscription.Id, CancellationToken.None);
                
                if (proposal != null && !string.IsNullOrEmpty(proposal.Title))
                {
                    return proposal.Title;
                }

                // Si no hay propuesta, usar información genérica
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                
                return $"Trabajo de {modality?.Name ?? "modalidad"} - Inscripción #{inscription.Id}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting project title for InscriptionModality ID: {InscriptionId}", inscription?.Id);
                return $"Proyecto - Inscripción #{inscription?.Id ?? 0}";
            }
        }
    }
}
