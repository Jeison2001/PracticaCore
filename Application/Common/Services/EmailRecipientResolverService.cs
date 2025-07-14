using Domain.Entities;
using Domain.Interfaces;
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
                // Obtener email directamente de los datos del evento
                var emailKey = rule.RuleValue.ToUpper() switch
                {
                    "STUDENT" => "StudentEmail",
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
            // Implementar lógica para obtener emails de estudiantes asignados
            var emails = new List<string>();
            
            if (eventData.ContainsKey("StudentId"))
            {
                var studentId = Convert.ToInt32(eventData["StudentId"]);
                var userRepo = _unitOfWork.GetRepository<User, int>();
                var student = await userRepo.GetByIdAsync(studentId);
                
                if (student != null && !string.IsNullOrEmpty(student.Email))
                {
                    emails.Add(student.Email);
                }
            }

            return emails;
        }
    }
}
