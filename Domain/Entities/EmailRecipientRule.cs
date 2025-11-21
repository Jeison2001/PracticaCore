namespace Domain.Entities
{
    /// <summary>
    /// Reglas para determinar los destinatarios de una notificación
    /// </summary>
    public class EmailRecipientRule : BaseEntity<int>
    {
        /// <summary>
        /// ID de la configuración de email padre
        /// </summary>
        public int EmailNotificationConfigId { get; set; }
        public EmailNotificationConfig EmailNotificationConfig { get; set; } = null!;

        /// <summary>
        /// Tipo de destinatario: "TO", "CC", "BCC"
        /// </summary>
        public string RecipientType { get; set; } = "TO";

        /// <summary>
        /// Tipo de regla para obtener destinatarios
        /// Ej: "BY_ROLE", "BY_ENTITY_RELATION", "FIXED_EMAIL", "EVENT_PARTICIPANT"
        /// </summary>
        public string RuleType { get; set; } = string.Empty;

        /// <summary>
        /// Valor de la regla (depende del RuleType)
        /// - BY_ROLE: "DIRECTOR", "COORDINATOR", "STUDENT"
        /// - BY_ENTITY_RELATION: "PROPOSAL_DIRECTOR", "FACULTY_COORDINATOR"
        /// - FIXED_EMAIL: "admin@universidad.edu"
        /// - EVENT_PARTICIPANT: "STUDENT", "DIRECTOR_ASSIGNED"
        /// </summary>
        public string RuleValue { get; set; } = string.Empty;

        /// <summary>
        /// Condiciones adicionales en JSON
        /// Ej: {"FacultyId": "{EntityFacultyId}", "IsActive": true}
        /// </summary>
        public string? Conditions { get; set; }

        /// <summary>
        /// Orden de prioridad para procesar las reglas
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}
