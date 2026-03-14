namespace Domain.Common.Notifications
{
    /// <summary>
    /// Resultado de la resolución de destinatarios
    /// </summary>
    public class EmailRecipientsResult
    {
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
    }
}
