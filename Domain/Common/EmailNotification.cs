namespace Domain.Common
{
    /// <summary>
    /// Representa una notificación por correo electrónico
    /// </summary>
    public class EmailNotification
    {
        /// <summary>
        /// Dirección de correo del destinatario principal
        /// </summary>
        public string To { get; set; } = string.Empty;

        /// <summary>
        /// Asunto del correo electrónico
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Cuerpo del mensaje
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el cuerpo del mensaje es HTML
        /// </summary>
        public bool IsHtml { get; set; } = true;

        /// <summary>
        /// Lista de destinatarios en copia (CC)
        /// </summary>
        public List<string> Cc { get; set; } = new();

        /// <summary>
        /// Lista de destinatarios en copia oculta (BCC)
        /// </summary>
        public List<string> Bcc { get; set; } = new();

        /// <summary>
        /// Lista de archivos adjuntos
        /// </summary>
        public List<EmailAttachment> Attachments { get; set; } = new();

        /// <summary>
        /// Dirección de correo del remitente (opcional, usa configuración por defecto si no se especifica)
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Nombre del remitente (opcional)
        /// </summary>
        public string? FromName { get; set; }

        /// <summary>
        /// Plantilla a usar para el envío (opcional)
        /// </summary>
        public string? TemplateId { get; set; }

        /// <summary>
        /// Datos dinámicos para la plantilla (opcional)
        /// </summary>
        public Dictionary<string, object> TemplateData { get; set; } = new();

        /// <summary>
        /// Prioridad del mensaje
        /// </summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    }

    /// <summary>
    /// Representa un archivo adjunto de email
    /// </summary>
    public class EmailAttachment
    {
        /// <summary>
        /// Nombre del archivo
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Contenido del archivo en bytes
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Tipo de contenido MIME
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";
    }

    /// <summary>
    /// Prioridad del email
    /// </summary>
    public enum EmailPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }
}
