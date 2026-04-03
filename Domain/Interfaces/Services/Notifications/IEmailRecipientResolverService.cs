using Domain.Common.Notifications;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications
{
    /// <summary>
    /// Resuelve destinatarios de email desde EmailRecipientRule (BY_ROLE, BY_PERMISSION,
    /// BY_ENTITY_RELATION, FIXED_EMAIL). Consulta BD para expandir grupos y devuelve
    /// listas separadas de TO, CC y BCC.
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
