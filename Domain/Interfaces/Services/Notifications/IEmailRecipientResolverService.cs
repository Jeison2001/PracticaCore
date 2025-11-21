using Domain.Common.Notifications;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications
{
    /// <summary>
    /// Servicio para resolver destinatarios basado en reglas
    /// </summary>
    public interface IEmailRecipientResolverService : IScopedService
    {
        /// <summary>
        /// Resuelve los destinatarios según las reglas configuradas
        /// </summary>
        Task<EmailRecipientsResult> ResolveRecipientsAsync(
            List<EmailRecipientRule> rules, 
            Dictionary<string, object> eventData, 
            object? entityContext = null);
    }
}
