namespace Domain.Common.Notifications
{
    /// <summary>
    /// Representa un archivo adjunto de email
    /// </summary>
    public record EmailAttachment
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
}
