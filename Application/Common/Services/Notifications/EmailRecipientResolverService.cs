using Domain.Common.Notifications;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Common.Services.Notifications
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

            foreach (var rule in rules.Where(r => r.StatusRegister).OrderBy(r => r.Priority))
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

            // Remover duplicados (case-insensitive)
            result.To = result.To
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            result.Cc = result.Cc
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            result.Bcc = result.Bcc
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result;
        }

        private async Task<List<string>> ResolveRuleAsync(EmailRecipientRule rule, Dictionary<string, object> eventData, object? entityContext)
        {
            return rule.RuleType.ToUpper() switch
            {
                "BY_ROLE" => await ResolveByRoleAsync(rule, eventData),
                "BY_PERMISSION" => await ResolveByPermissionAsync(rule, eventData),
                "BY_ENTITY_RELATION" => await ResolveByEntityRelationAsync(rule, eventData, entityContext),
                "FIXED_EMAIL" => ResolveFixedEmail(rule),
                "EVENT_PARTICIPANT" => ResolveEventParticipant(rule, eventData),
                _ => new List<string>()
            };
        }

        private Task<List<string>> ResolveByRoleAsync(EmailRecipientRule rule, Dictionary<string, object> eventData) =>
            ResolveByRoleCodeAsync(rule.RuleValue, rule.Conditions, eventData);

        private async Task<List<string>> ResolveByRoleCodeAsync(
            string roleCode,
            string? conditions,
            Dictionary<string, object> eventData)
        {
            var emails = new List<string>();
            
            try
            {
                // Obtener usuarios por rol
                var userRepo = _unitOfWork.GetRepository<User, int>();
                var roleRepo = _unitOfWork.GetRepository<Role, int>();
                var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();

                if (roleRepo == null || userRoleRepo == null || userRepo == null)
                    return emails;

                // Buscar rol activo por código (más estable que por nombre)
                var role = await roleRepo.GetFirstOrDefaultAsync(
                    r => r.Code == roleCode && r.StatusRegister,
                    CancellationToken.None);
                if (role == null) 
                {
                    _logger.LogWarning("No se encontró rol con código: {RoleCode}", roleCode);
                    return emails;
                }

                // Obtener usuarios con asignación de rol activa
                var userRoles = await userRoleRepo.GetAllAsync(filter: ur => ur.IdRole == role.Id && ur.StatusRegister);
                var userIds = userRoles.Select(ur => ur.IdUser).ToList();

                var users = await userRepo.GetAllAsync(filter: u => userIds.Contains(u.Id) && u.StatusRegister);
                
                // Aplicar condiciones adicionales si existen
                if (!string.IsNullOrEmpty(conditions))
                {
                    users = await ApplyAdditionalConditionsAsync(users, conditions, eventData);
                }

                emails.AddRange(users.Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolviendo destinatarios por rol: {RoleValue}", roleCode);
            }

            return emails;
        }

        private Task<List<string>> ResolveByPermissionAsync(EmailRecipientRule rule, Dictionary<string, object> eventData) =>
            ResolveByPermissionCodeAsync(rule.RuleValue, rule.Conditions, eventData);

        private async Task<List<string>> ResolveByPermissionCodeAsync(
            string permissionCode,
            string? conditions,
            Dictionary<string, object> eventData)
        {
            var emails = new List<string>();

            try
            {
                var userRepo = _unitOfWork.GetRepository<User, int>();
                var permissionRepo = _unitOfWork.GetRepository<Permission, int>();
                var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();

                if (permissionRepo == null || userPermissionRepo == null || userRepo == null)
                    return emails;

                var permission = await permissionRepo.GetFirstOrDefaultAsync(
                    p => p.Code == permissionCode && p.StatusRegister,
                    CancellationToken.None);

                if (permission == null)
                {
                    _logger.LogWarning("No se encontró permiso con código: {PermissionCode}", permissionCode);
                    return emails;
                }

                var userPermissions = await userPermissionRepo.GetAllAsync(
                    filter: up => up.IdPermission == permission.Id && up.StatusRegister);
                var targetUserIds = userPermissions.Select(up => up.IdUser).ToHashSet();

                // Regla fallback idéntica a UserInfoRepository: usuarios activos sin NINGUNA fila en UserPermission
                // heredan permisos desde RolePermission a través de UserRole y Role activos.
                var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();
                var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
                var roleRepo = _unitOfWork.GetRepository<Role, int>();

                if (rolePermissionRepo != null && userRoleRepo != null)
                {
                    var rolePermissions = await rolePermissionRepo.GetAllAsync(
                        filter: rp => rp.IdPermission == permission.Id && rp.StatusRegister);
                    var roleIds = rolePermissions.Select(rp => rp.IdRole).Distinct().ToList();

                    if (roleIds.Count > 0)
                    {
                        HashSet<int> activeRoleIds;
                        if (roleRepo != null)
                        {
                            var activeRoles = await roleRepo.GetAllAsync(
                                filter: r => roleIds.Contains(r.Id) && r.StatusRegister);
                            activeRoleIds = activeRoles.Select(r => r.Id).ToHashSet();
                        }
                        else
                        {
                            activeRoleIds = roleIds.ToHashSet();
                        }

                        if (activeRoleIds.Count > 0)
                        {
                            var candidateUserRoles = await userRoleRepo.GetAllAsync(
                                filter: ur => activeRoleIds.Contains(ur.IdRole) && ur.StatusRegister);
                            var candidateUserIds = candidateUserRoles
                                .Select(ur => ur.IdUser)
                                .Where(uid => !targetUserIds.Contains(uid))
                                .Distinct()
                                .ToList();

                            if (candidateUserIds.Count > 0)
                            {
                                var existingUserPermRows = await userPermissionRepo.GetAllAsync(
                                    filter: up => candidateUserIds.Contains(up.IdUser));
                                var usersWithAnyUserPermission = existingUserPermRows
                                    .Select(up => up.IdUser)
                                    .ToHashSet();

                                foreach (var uid in candidateUserIds.Where(id => !usersWithAnyUserPermission.Contains(id)))
                                {
                                    targetUserIds.Add(uid);
                                }
                            }
                        }
                    }
                }

                if (targetUserIds.Count == 0)
                    return emails;

                var users = await userRepo.GetAllAsync(
                    filter: u => targetUserIds.Contains(u.Id) && u.StatusRegister);

                if (!string.IsNullOrEmpty(conditions))
                {
                    users = await ApplyAdditionalConditionsAsync(users, conditions, eventData);
                }

                emails.AddRange(users
                    .Select(u => u.Email)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolviendo destinatarios por permiso: {PermissionCode}", permissionCode);
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
                    case "PRACTICE_STAGE_RESPONSIBLE":
                        emails.AddRange(await ResolvePracticeStageResponsibleAsync(rule, eventData));
                        break;
                    case "STAGE_EVALUATOR_RESPONSIBLE":
                        emails.AddRange(await ResolveStageEvaluatorResponsibleAsync(rule, eventData));
                        break;
                    case "COMMITTEE_STAGE_RESPONSIBLE":
                        emails.AddRange(await ResolveCommitteeStageResponsibleAsync(rule, eventData));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolviendo destinatarios por relación de entidad: {RuleValue}", rule.RuleValue);
            }

            return emails;
        }

        private static bool TryExtractInscriptionModalityId(
            Dictionary<string, object> eventData,
            out int inscriptionModalityId)
        {
            inscriptionModalityId = 0;

            if (eventData.TryGetValue("InscriptionModalityId", out var rawImId) && rawImId != null)
            {
                inscriptionModalityId = Convert.ToInt32(rawImId);
            }
            else if (eventData.TryGetValue("InscriptionId", out var rawInsId) && rawInsId != null)
            {
                inscriptionModalityId = Convert.ToInt32(rawInsId);
            }

            return inscriptionModalityId > 0;
        }

        private static Dictionary<string, object> EnsureInscriptionModalityIdInData(
            Dictionary<string, object> eventData,
            int inscriptionModalityId)
        {
            if (eventData.ContainsKey("InscriptionModalityId"))
                return eventData;

            var clone = new Dictionary<string, object>(eventData)
            {
                ["InscriptionModalityId"] = inscriptionModalityId
            };
            return clone;
        }

        private async Task<List<string>> ResolveStageEvaluatorResponsibleAsync(
            EmailRecipientRule rule,
            Dictionary<string, object> eventData)
        {
            var emails = new List<string>();

            if (!TryExtractInscriptionModalityId(eventData, out var inscriptionModalityId))
            {
                _logger.LogWarning("No se encontró InscriptionModalityId ni InscriptionId válido en eventData para STAGE_EVALUATOR_RESPONSIBLE.");
                return emails;
            }

            var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();

            var inscription = inscriptionRepo != null ? await inscriptionRepo.GetByIdAsync(inscriptionModalityId) : null;
            if (inscription == null)
            {
                _logger.LogWarning("No se encontró InscriptionModality con ID {Id} para STAGE_EVALUATOR_RESPONSIBLE.", inscriptionModalityId);
                return emails;
            }

            var modality = inscription.Modality;
            if (modality == null && modalityRepo != null)
            {
                modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
            }

            var modalityCode = modality?.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            var lookupData = EnsureInscriptionModalityIdInData(eventData, inscriptionModalityId);

            if (modalityCode == ModalityCodes.ProyectoGrado)
            {
                emails.AddRange(await GetTeachersByAssignmentTypeAsync(lookupData, "JURADO_EVALUADOR"));
            }
            else if (modalityCode == ModalityCodes.PracticaAcademica)
            {
                emails.AddRange(await GetConfirmedPracticeTeachersEmailsAsync(inscriptionModalityId));
            }

            return emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<List<string>> ResolveCommitteeStageResponsibleAsync(
            EmailRecipientRule rule,
            Dictionary<string, object> eventData)
        {
            var emails = new List<string>();

            if (!TryExtractInscriptionModalityId(eventData, out var inscriptionModalityId))
            {
                _logger.LogWarning("No se encontró InscriptionModalityId ni InscriptionId válido en eventData para COMMITTEE_STAGE_RESPONSIBLE.");
                return emails;
            }

            var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();
            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();

            var inscription = inscriptionRepo != null ? await inscriptionRepo.GetByIdAsync(inscriptionModalityId) : null;
            if (inscription == null)
            {
                _logger.LogWarning("No se encontró InscriptionModality con ID {Id} para COMMITTEE_STAGE_RESPONSIBLE.", inscriptionModalityId);
                return emails;
            }

            var modality = inscription.Modality;
            if (modality == null && modalityRepo != null)
            {
                modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
            }

            var stateInscription = inscription.StateInscription;
            if (stateInscription == null && stateInscriptionRepo != null)
            {
                stateInscription = await stateInscriptionRepo.GetByIdAsync(inscription.IdStateInscription);
            }

            var stageModality = inscription.StageModality;
            if (stageModality == null && inscription.IdStageModality.HasValue && stageModalityRepo != null)
            {
                stageModality = await stageModalityRepo.GetByIdAsync(inscription.IdStageModality.Value);
            }

            var modalityCode = modality?.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            var stateInscriptionCode = stateInscription?.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            var stageCode = stageModality?.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            var lookupData = EnsureInscriptionModalityIdInData(eventData, inscriptionModalityId);

            if (stateInscriptionCode == StateInscriptionCodes.Pendiente)
            {
                emails.AddRange(await ResolveByPermissionCodeAsync(
                    PermissionCodes.ModalidadesGrado.N3MGPR,
                    rule.Conditions,
                    lookupData));
            }
            else if (modalityCode == ModalityCodes.PracticaAcademica)
            {
                emails.AddRange(await ResolvePracticeStageResponsibleAsync(rule, lookupData));
            }
            else
            {
                var permCode = (modalityCode, stageCode) switch
                {
                    (ModalityCodes.ProyectoGrado, StageModalityCodes.PgFasePropuesta) => PermissionCodes.ProyectoGrado.N4PGPPR,
                    (ModalityCodes.ProyectoGrado, StageModalityCodes.PgFaseAnteproyecto) => PermissionCodes.ProyectoGrado.N4PGPAA,
                    (ModalityCodes.ProyectoGrado, StageModalityCodes.PgFaseSustentacion) => PermissionCodes.ProyectoGrado.N4PGPSG,
                    (ModalityCodes.ProyectoGrado, _) => PermissionCodes.ProyectoGrado.N4PGPPRG,
                    (ModalityCodes.PublicacionArticulo, _) => PermissionCodes.PublicacionArticulo.N3PCSGR,
                    (ModalityCodes.SaberPro, _) => PermissionCodes.SaberPro.N3SPSGR,
                    (ModalityCodes.GradoPromedio, _) => PermissionCodes.GradoPromedio.N3GPSGR,
                    (ModalityCodes.CoTerminal, _) => PermissionCodes.CoTerminal.N3CTSGR,
                    (ModalityCodes.SeminarioAct, _) => PermissionCodes.Seminario.N3SASGR,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(permCode))
                {
                    emails.AddRange(await ResolveByPermissionCodeAsync(permCode, rule.Conditions, lookupData));
                }
            }

            return emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<List<string>> ResolvePracticeStageResponsibleAsync(
            EmailRecipientRule rule,
            Dictionary<string, object> eventData)
        {
            var emails = new List<string>();

            int inscriptionModalityId = 0;
            if (eventData.TryGetValue("InscriptionModalityId", out var rawImId) && rawImId != null)
            {
                inscriptionModalityId = Convert.ToInt32(rawImId);
            }
            else if (eventData.TryGetValue("InscriptionId", out var rawInsId) && rawInsId != null)
            {
                inscriptionModalityId = Convert.ToInt32(rawInsId);
            }

            if (inscriptionModalityId <= 0)
            {
                _logger.LogWarning("No se encontró InscriptionModalityId ni InscriptionId válido en eventData para PRACTICE_STAGE_RESPONSIBLE.");
                return emails;
            }

            var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();

            var inscription = inscriptionRepo != null ? await inscriptionRepo.GetByIdAsync(inscriptionModalityId) : null;
            if (inscription == null)
            {
                _logger.LogWarning("No se encontró InscriptionModality con ID {Id} para PRACTICE_STAGE_RESPONSIBLE.", inscriptionModalityId);
                return emails;
            }

            var stageModality = inscription.StageModality;
            if (stageModality == null && inscription.IdStageModality.HasValue && stageModalityRepo != null)
            {
                stageModality = await stageModalityRepo.GetByIdAsync(inscription.IdStageModality.Value);
            }

            var stageCode = stageModality?.Code?.Trim().ToUpperInvariant() ?? string.Empty;

            switch (stageCode)
            {
                case StageModalityCodes.PaFaseInscripcion:
                    emails.AddRange(await ResolveByPermissionCodeAsync(
                        PermissionCodes.PracticaAcademica.N4PAF0PA,
                        rule.Conditions,
                        eventData));
                    break;

                case StageModalityCodes.PaFaseDesarrollo:
                    emails.AddRange(await GetConfirmedPracticeTeachersEmailsAsync(inscriptionModalityId));
                    emails.AddRange(await ResolveByPermissionCodeAsync(
                        PermissionCodes.PracticaAcademica.N4PAF1PG,
                        rule.Conditions,
                        eventData));
                    break;

                case StageModalityCodes.PaFaseEvaluacion:
                default:
                    emails.AddRange(await GetConfirmedPracticeTeachersEmailsAsync(inscriptionModalityId));
                    break;
            }

            return emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<List<string>> GetConfirmedPracticeTeachersEmailsAsync(int inscriptionModalityId)
        {
            var emails = new List<string>();
            var teachingAssignmentRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
            var userRepo = _unitOfWork.GetRepository<User, int>();

            if (teachingAssignmentRepo == null || userRepo == null)
                return emails;

            var assignments = await teachingAssignmentRepo.GetAllAsync(
                filter: ta => ta.IdInscriptionModality == inscriptionModalityId &&
                              ta.StatusRegister &&
                              ta.RevocationDate == null &&
                              ta.IsConfirmed);

            var teacherIds = assignments.Select(ta => ta.IdTeacher).Distinct().ToList();
            if (teacherIds.Count == 0)
                return emails;

            var teachers = await userRepo.GetAllAsync(
                filter: u => teacherIds.Contains(u.Id) && u.StatusRegister);

            emails.AddRange(teachers
                .Select(t => t.Email)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim()));

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
                            
                            _logger.LogInformation("Resolviendo emails de estudiantes: {Count} emails encontrados", emailList.Count);
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
                            _logger.LogInformation("Usando email individual de estudiante: {Email}", email);
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
                        "USER" => "UserEmail",
                        "NEW_USER" => "UserEmail",
                        "INVITED_STUDENT" => "InvitedStudentEmail",
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
            try
            {
                var conditionsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(conditions);
                if (conditionsDict == null)
                    return Enumerable.Empty<User>();

                if (conditionsDict.ContainsKey("FacultyId"))
                {
                    int facultyId;
                    if (eventData.TryGetValue("FacultyId", out var rawEventFacultyId) && rawEventFacultyId != null)
                    {
                        facultyId = Convert.ToInt32(rawEventFacultyId);
                    }
                    else if (conditionsDict["FacultyId"] is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var literalFacultyId))
                    {
                        facultyId = literalFacultyId;
                    }
                    else
                    {
                        return Enumerable.Empty<User>();
                    }

                    var userList = users.ToList();
                    var programIds = userList.Select(u => u.IdAcademicProgram).Where(id => id > 0).Distinct().ToList();
                    var programRepo = _unitOfWork.GetRepository<AcademicProgram, int>();
                    var matchingProgramIds = (await programRepo.GetAllAsync(
                            p => programIds.Contains(p.Id) && p.IdFaculty == facultyId && p.StatusRegister))
                        .Select(p => p.Id)
                        .ToHashSet();

                    users = userList.Where(u =>
                        (u.AcademicProgram != null && u.AcademicProgram.IdFaculty == facultyId) ||
                        matchingProgramIds.Contains(u.IdAcademicProgram));
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error aplicando condiciones adicionales de destinatarios; se retorna lista vacía (fail-closed).");
                return Enumerable.Empty<User>();
            }
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
                    
                    _logger.LogInformation("Emails de estudiantes asignados: {Count} emails encontrados", emailList.Count);
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
                
                if (student != null && student.StatusRegister && !string.IsNullOrEmpty(student.Email))
                {
                    emails.Add(student.Email);
                    _logger.LogInformation("Email individual de estudiante por ID: {Email}", student.Email);
                }
            }
            
            // Fallback: usar email individual si no hay múltiples ni ID
            if (!emails.Any() && eventData.ContainsKey("StudentEmail"))
            {
                var email = eventData["StudentEmail"]?.ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    emails.Add(email);
                    _logger.LogInformation("Email individual de estudiante (fallback): {Email}", email);
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
                    _logger.LogInformation("Email del docente asignado: {Email}", teacherEmail);
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
                    _logger.LogInformation("Email del docente desasignado: {Email}", teacherEmail);
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
                    _logger.LogWarning("No se encontró InscriptionModalityId en eventData para obtener docentes del tipo: {AssignmentType}", assignmentTypeCode);
                    return emails;
                }

                var inscriptionModalityId = Convert.ToInt32(eventData["InscriptionModalityId"]);
                
                var teachingAssignmentRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
                var typeTeachingAssignmentRepo = _unitOfWork.GetRepository<TypeTeachingAssignment, int>();
                var userRepo = _unitOfWork.GetRepository<User, int>();

                // Buscar el tipo de asignación por código
                var assignmentType = await typeTeachingAssignmentRepo.GetFirstOrDefaultAsync(
                    tta => tta.Code == assignmentTypeCode && tta.StatusRegister, 
                    CancellationToken.None);

                if (assignmentType == null)
                {
                    _logger.LogWarning("No se encontró TypeTeachingAssignment con código: {AssignmentTypeCode}", assignmentTypeCode);
                    return emails;
                }

                // Obtener asignaciones de docentes activas, confirmadas y no revocadas para esta inscripción y tipo
                var assignments = await teachingAssignmentRepo.GetAllAsync(
                    filter: ta => ta.IdInscriptionModality == inscriptionModalityId && 
                                 ta.IdTypeTeachingAssignment == assignmentType.Id &&
                                 ta.StatusRegister &&
                                 ta.IsConfirmed &&
                                 ta.RevocationDate == null);

                if (!assignments.Any())
                {
                    _logger.LogWarning("No se encontraron asignaciones del tipo {AssignmentType} para InscriptionModalityId: {Id}", 
                        assignmentTypeCode, inscriptionModalityId);
                    return emails;
                }

                var teacherIds = assignments.Select(ta => ta.IdTeacher).ToList();
                var teachers = await userRepo.GetAllAsync(
                    filter: u => teacherIds.Contains(u.Id) && u.StatusRegister);

                emails.AddRange(teachers.Select(t => t.Email).Where(email => !string.IsNullOrEmpty(email)));
                
                _logger.LogInformation("Docentes del tipo {AssignmentType} encontrados: {Count} emails", 
                    assignmentTypeCode, emails.Count);
                
                foreach (var email in emails)
                {
                    _logger.LogInformation("  → {AssignmentType}: {Email}", assignmentTypeCode, email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo emails de docentes del tipo: {AssignmentType}", assignmentTypeCode);
            }

            return emails;
        }
    }
}
