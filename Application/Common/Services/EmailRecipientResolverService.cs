using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Common.Services
{
    /// <summary>
    /// Servicio para resolver destinatarios basado en reglas de negocio
    /// </summary>
    public class EmailRecipientResolverService : IEmailRecipientResolverService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EmailRecipientResolverService> _logger;

        public EmailRecipientResolverService(IUnitOfWork unitOfWork, ILogger<EmailRecipientResolverService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EmailRecipientsResult> ResolveRecipientsAsync(
            List<EmailRecipientRule> rules, 
            Dictionary<string, object> eventData, 
            object? entityContext = null)
        {
            var result = new EmailRecipientsResult();

            foreach (var rule in rules.OrderBy(r => r.Priority))
            {
                try
                {
                    var emails = await ResolveRuleAsync(rule, eventData, entityContext);
                    
                    switch (rule.RecipientType.ToUpper())
                    {
                        case "TO":
                            result.To.AddRange(emails);
                            break;
                        case "CC":
                            result.Cc.AddRange(emails);
                            break;
                        case "BCC":
                            result.Bcc.AddRange(emails);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolviendo regla {RuleId}: {RuleType} - {RuleValue}", 
                        rule.Id, rule.RuleType, rule.RuleValue);
                }
            }

            // Remover duplicados
            result.To = result.To.Distinct().ToList();
            result.Cc = result.Cc.Distinct().ToList();
            result.Bcc = result.Bcc.Distinct().ToList();

            return result;
        }

        private async Task<List<string>> ResolveRuleAsync(EmailRecipientRule rule, Dictionary<string, object> eventData, object? entityContext)
        {
            return rule.RuleType.ToUpper() switch
            {
                "BY_ROLE" => await ResolveByRoleAsync(rule, eventData),
                "BY_ENTITY_RELATION" => await ResolveByEntityRelationAsync(rule, eventData, entityContext),
                "FIXED_EMAIL" => ResolveFixedEmail(rule),
                "EVENT_PARTICIPANT" => ResolveEventParticipant(rule, eventData),
                _ => new List<string>()
            };
        }

        private async Task<List<string>> ResolveByRoleAsync(EmailRecipientRule rule, Dictionary<string, object> eventData)
        {
            var emails = new List<string>();
            
            try
            {
                // Obtener usuarios por rol
                var userRepo = _unitOfWork.GetRepository<User, int>();
                var roleRepo = _unitOfWork.GetRepository<Role, int>();
                var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();

                // Buscar rol por código (más estable que por nombre)
                var role = await roleRepo.GetFirstOrDefaultAsync(r => r.Code == rule.RuleValue, CancellationToken.None);
                if (role == null) 
                {
                    _logger.LogWarning("No se encontró rol con código: {RoleCode}", rule.RuleValue);
                    return emails;
                }

                // Obtener usuarios con ese rol
                var userRoles = await userRoleRepo.GetAllAsync(filter: ur => ur.IdRole == role.Id);
                var userIds = userRoles.Select(ur => ur.IdUser).ToList();

                var users = await userRepo.GetAllAsync(filter: u => userIds.Contains(u.Id) && u.StatusRegister);
                
                // Aplicar condiciones adicionales si existen
                if (!string.IsNullOrEmpty(rule.Conditions))
                {
                    users = await ApplyAdditionalConditionsAsync(users, rule.Conditions, eventData);
                }

                emails.AddRange(users.Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolviendo destinatarios por rol: {RoleValue}", rule.RuleValue);
            }

            return emails;
        }

        private async Task<List<string>> ResolveByEntityRelationAsync(EmailRecipientRule rule, Dictionary<string, object> eventData, object? entityContext)
        {
            var emails = new List<string>();

            try
            {
                switch (rule.RuleValue.ToUpper())
                {
                    case "PROPOSAL_DIRECTOR":
                        emails.AddRange(await GetProposalDirectorEmailsAsync(eventData));
                        break;
                    case "FACULTY_COORDINATOR":
                        emails.AddRange(await GetFacultyCoordinatorEmailsAsync(eventData));
                        break;
                    case "STUDENT_ASSIGNED":
                        emails.AddRange(await GetStudentAssignedEmailsAsync(eventData));
                        break;
                    case "ASSIGNED_TEACHER":
                        emails.AddRange(await GetAssignedTeacherEmailsAsync(eventData));
                        break;
                    case "UNASSIGNED_TEACHER":
                        emails.AddRange(await GetUnassignedTeacherEmailsAsync(eventData));
                        break;
                    case "EVALUATOR_ASSIGNED":
                        emails.AddRange(await GetTeachersByAssignmentTypeAsync(eventData, "EVALUADOR"));
                        break;
                    case "JURY_EVALUATOR_ASSIGNED":
                        emails.AddRange(await GetTeachersByAssignmentTypeAsync(eventData, "JURADO_EVALUADOR"));
                        break;
                    case "DIRECTOR_ASSIGNED":
                        emails.AddRange(await GetTeachersByAssignmentTypeAsync(eventData, "DIRECTOR"));
                        break;
                    case "CO_DIRECTOR_ASSIGNED":
                        emails.AddRange(await GetTeachersByAssignmentTypeAsync(eventData, "CO_DIRECTOR"));
                        break;
                    case "ASESOR_ASSIGNED":
                        emails.AddRange(await GetTeachersByAssignmentTypeAsync(eventData, "ASESOR"));
                        break;
                    // Agregar más casos según necesidades
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolviendo destinatarios por relación de entidad: {RuleValue}", rule.RuleValue);
            }

            return emails;
        }

        private List<string> ResolveFixedEmail(EmailRecipientRule rule)
        {
            // Email fijo configurado
            return new List<string> { rule.RuleValue };
        }

        private List<string> ResolveEventParticipant(EmailRecipientRule rule, Dictionary<string, object> eventData)
        {
            var emails = new List<string>();

            try
            {
                // Para STUDENT, manejar tanto emails individuales como múltiples
                if (rule.RuleValue.ToUpper() == "STUDENT")
                {
                    // Primero intentar obtener todos los emails (para múltiples estudiantes)
                    if (eventData.ContainsKey("StudentEmails"))
                    {
                        var studentEmails = eventData["StudentEmails"]?.ToString();
                        if (!string.IsNullOrEmpty(studentEmails))
                        {
                            // Si hay múltiples emails separados por coma, dividirlos
                            var emailList = studentEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(e => e.Trim())
                                                       .Where(e => !string.IsNullOrEmpty(e))
                                                       .ToList();
                            emails.AddRange(emailList);
                            
                            _logger.LogInformation("📧 Resolviendo emails de estudiantes: {Count} emails encontrados", emailList.Count);
                            foreach (var email in emailList)
                            {
                                _logger.LogInformation("  → {Email}", email);
                            }
                        }
                    }
                    
                    // Fallback: usar email individual si no hay múltiples
                    if (!emails.Any() && eventData.ContainsKey("StudentEmail"))
                    {
                        var email = eventData["StudentEmail"]?.ToString();
                        if (!string.IsNullOrEmpty(email))
                        {
                            emails.Add(email);
                            _logger.LogInformation("📧 Usando email individual de estudiante: {Email}", email);
                        }
                    }
                }
                else
                {
                    // Para otros tipos de participantes, usar la lógica original
                    var emailKey = rule.RuleValue.ToUpper() switch
                    {
                        "DIRECTOR" => "DirectorEmail",
                        "COORDINATOR" => "CoordinatorEmail",
                        _ => rule.RuleValue
                    };

                    if (eventData.ContainsKey(emailKey))
                    {
                        var email = eventData[emailKey]?.ToString();
                        if (!string.IsNullOrEmpty(email))
                        {
                            emails.Add(email);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolviendo participante del evento: {RuleValue}", rule.RuleValue);
            }

            return emails;
        }
                   

        private async Task<IEnumerable<User>> ApplyAdditionalConditionsAsync(IEnumerable<User> users, string conditions, Dictionary<string, object> eventData)
        {
            // Implementar lógica para aplicar condiciones adicionales en JSON
            // Por ejemplo, filtrar por facultad, programa académico, etc.
            
            try
            {
                var conditionsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(conditions);
                if (conditionsDict == null) return users;

                // Ejemplo: filtrar por facultad si está en las condiciones
                if (conditionsDict.ContainsKey("FacultyId") && eventData.ContainsKey("FacultyId"))
                {
                    var facultyId = Convert.ToInt32(eventData["FacultyId"]);
                    // Aplicar filtro por facultad (esto requeriría relación User -> AcademicProgram -> Faculty)
                    // users = users.Where(u => u.AcademicProgram?.FacultyId == facultyId);
                }

                return users;
            }
            catch
            {
                return users; // Si hay error en condiciones, devolver todos los usuarios
            }
        }

        private async Task<List<string>> GetProposalDirectorEmailsAsync(Dictionary<string, object> eventData)
        {
            // Implementar lógica para obtener emails de directores de propuesta
            var emails = new List<string>();
            
            if (eventData.ContainsKey("ProposalId"))
            {
                var proposalId = Convert.ToInt32(eventData["ProposalId"]);
                // Consultar director(es) asignado(s) a la propuesta
                // var directors = await GetDirectorsByProposalId(proposalId);
                // emails.AddRange(directors.Select(d => d.Email));
            }

            return emails;
        }

        private async Task<List<string>> GetFacultyCoordinatorEmailsAsync(Dictionary<string, object> eventData)
        {
            // Implementar lógica para obtener emails de coordinadores de facultad
            var emails = new List<string>();
            
            if (eventData.ContainsKey("FacultyId"))
            {
                var facultyId = Convert.ToInt32(eventData["FacultyId"]);
                // Consultar coordinadores de la facultad
                // var coordinators = await GetCoordinatorsByFacultyId(facultyId);
                // emails.AddRange(coordinators.Select(c => c.Email));
            }

            return emails;
        }

        private async Task<List<string>> GetStudentAssignedEmailsAsync(Dictionary<string, object> eventData)
        {
            var emails = new List<string>();
            
            // Manejo de múltiples estudiantes (nuevo - para asignaciones docentes)
            if (eventData.ContainsKey("StudentEmails"))
            {
                var studentEmails = eventData["StudentEmails"]?.ToString();
                if (!string.IsNullOrEmpty(studentEmails))
                {
                    var emailList = studentEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(e => e.Trim())
                                               .Where(e => !string.IsNullOrEmpty(e))
                                               .ToList();
                    emails.AddRange(emailList);
                    
                    _logger.LogInformation("📧 Emails de estudiantes asignados: {Count} emails encontrados", emailList.Count);
                    foreach (var email in emailList)
                    {
                        _logger.LogInformation("  → {Email}", email);
                    }
                }
            }
            
            // Manejo de estudiante individual (legacy - mantener compatibilidad)
            if (!emails.Any() && eventData.ContainsKey("StudentId"))
            {
                var studentId = Convert.ToInt32(eventData["StudentId"]);
                var userRepo = _unitOfWork.GetRepository<User, int>();
                var student = await userRepo.GetByIdAsync(studentId);
                
                if (student != null && !string.IsNullOrEmpty(student.Email))
                {
                    emails.Add(student.Email);
                    _logger.LogInformation("📧 Email individual de estudiante por ID: {Email}", student.Email);
                }
            }
            
            // Fallback: usar email individual si no hay múltiples ni ID
            if (!emails.Any() && eventData.ContainsKey("StudentEmail"))
            {
                var email = eventData["StudentEmail"]?.ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    emails.Add(email);
                    _logger.LogInformation("📧 Email individual de estudiante (fallback): {Email}", email);
                }
            }

            return emails;
        }

        private async Task<List<string>> GetAssignedTeacherEmailsAsync(Dictionary<string, object> eventData)
        {
            var emails = new List<string>();
            
            // Obtener email del docente asignado
            if (eventData.ContainsKey("TeacherEmail"))
            {
                var teacherEmail = eventData["TeacherEmail"]?.ToString();
                if (!string.IsNullOrEmpty(teacherEmail))
                {
                    emails.Add(teacherEmail);
                    _logger.LogInformation("📧 Email del docente asignado: {Email}", teacherEmail);
                }
            }

            return emails;
        }

        private async Task<List<string>> GetUnassignedTeacherEmailsAsync(Dictionary<string, object> eventData)
        {
            var emails = new List<string>();
            
            // Obtener email del docente que fue desasignado
            if (eventData.ContainsKey("UnassignedTeacherEmail"))
            {
                var teacherEmail = eventData["UnassignedTeacherEmail"]?.ToString();
                if (!string.IsNullOrEmpty(teacherEmail))
                {
                    emails.Add(teacherEmail);
                    _logger.LogInformation("📧 Email del docente desasignado: {Email}", teacherEmail);
                }
            }
            // Fallback usando TeacherEmail para compatibilidad
            else if (eventData.ContainsKey("TeacherEmail"))
            {
                var teacherEmail = eventData["TeacherEmail"]?.ToString();
                if (!string.IsNullOrEmpty(teacherEmail))
                {
                    emails.Add(teacherEmail);
                    _logger.LogInformation("📧 Email del docente (fallback): {Email}", teacherEmail);
                }
            }

            return emails;
        }

        /// <summary>
        /// Obtiene emails de docentes asignados según el tipo de asignación
        /// Método genérico que maneja DIRECTOR, EVALUADOR, JURADO_EVALUADOR, etc.
        /// </summary>
        /// <param name="eventData">Datos del evento que debe contener InscriptionModalityId</param>
        /// <param name="assignmentTypeCode">Código del tipo de asignación (DIRECTOR, EVALUADOR, JURADO_EVALUADOR, etc.)</param>
        /// <returns>Lista de emails de los docentes asignados</returns>
        private async Task<List<string>> GetTeachersByAssignmentTypeAsync(Dictionary<string, object> eventData, string assignmentTypeCode)
        {
            var emails = new List<string>();
            
            try
            {
                if (!eventData.ContainsKey("InscriptionModalityId"))
                {
                    _logger.LogWarning("⚠️ No se encontró InscriptionModalityId en eventData para obtener docentes del tipo: {AssignmentType}", assignmentTypeCode);
                    return emails;
                }

                var inscriptionModalityId = Convert.ToInt32(eventData["InscriptionModalityId"]);
                
                var teachingAssignmentRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
                var typeTeachingAssignmentRepo = _unitOfWork.GetRepository<TypeTeachingAssignment, int>();
                var userRepo = _unitOfWork.GetRepository<User, int>();

                // Buscar el tipo de asignación por código
                var assignmentType = await typeTeachingAssignmentRepo.GetFirstOrDefaultAsync(
                    tta => tta.Code == assignmentTypeCode, 
                    CancellationToken.None);

                if (assignmentType == null)
                {
                    _logger.LogWarning("⚠️ No se encontró TypeTeachingAssignment con código: {AssignmentTypeCode}", assignmentTypeCode);
                    return emails;
                }

                // Obtener asignaciones de docentes para esta inscripción y tipo
                var assignments = await teachingAssignmentRepo.GetAllAsync(
                    filter: ta => ta.IdInscriptionModality == inscriptionModalityId && 
                                 ta.IdTypeTeachingAssignment == assignmentType.Id &&
                                 ta.StatusRegister);

                if (!assignments.Any())
                {
                    _logger.LogWarning("⚠️ No se encontraron asignaciones del tipo {AssignmentType} para InscriptionModalityId: {Id}", 
                        assignmentTypeCode, inscriptionModalityId);
                    return emails;
                }

                var teacherIds = assignments.Select(ta => ta.IdTeacher).ToList();
                var teachers = await userRepo.GetAllAsync(
                    filter: u => teacherIds.Contains(u.Id) && u.StatusRegister);

                emails.AddRange(teachers.Select(t => t.Email).Where(email => !string.IsNullOrEmpty(email)));
                
                _logger.LogInformation("📧 Docentes del tipo {AssignmentType} encontrados: {Count} emails", 
                    assignmentTypeCode, emails.Count);
                
                foreach (var email in emails)
                {
                    _logger.LogInformation("  → {AssignmentType}: {Email}", assignmentTypeCode, email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo emails de docentes del tipo: {AssignmentType}", assignmentTypeCode);
            }

            return emails;
        }
    }
}
