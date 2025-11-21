namespace Domain.Entities
{
    /// <summary>
    /// Configuración de notificaciones por email para eventos del sistema
    /// </summary>
    public class EmailNotificationConfig : BaseEntity<int>
    {
        /// <summary>
        /// Nombre único del evento que activa la notificación
        /// Ej: "PROPOSAL_CREATED", "INSCRIPTION_APPROVED", etc.
        /// </summary>
        public string EventName { get; set; } = string.Empty;

        /// <summary>
        /// Plantilla del asunto del email (puede usar placeholders)
        /// Ej: "Propuesta {ProposalTitle} ha sido creada"
        /// </summary>
        public string SubjectTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Plantilla del cuerpo del email (HTML con placeholders)
        /// Ej: "Estimado {StudentName}, tu propuesta {ProposalTitle}..."
        /// </summary>
        public string BodyTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Indica si está activa esta configuración
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Reglas de destinatarios para este evento
        /// </summary>
        public List<EmailRecipientRule> RecipientRules { get; set; } = new();
    }
}
