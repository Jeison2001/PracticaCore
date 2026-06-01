namespace Domain.Configuration
{
    /// <summary>
    /// Opciones de configuración para autenticación con Microsoft Entra ID (Azure AD).
    /// </summary>
    public class AzureAdAuthOptions
    {
        /// <summary>
        /// Autoridad base. Por defecto el cloud público de Microsoft.
        /// </summary>
        public string Instance { get; set; } = "https://login.microsoftonline.com/";

        /// <summary>
        /// Identificador del tenant (directorio) de Entra ID.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// ClientId (Application ID) de la app registrada. Se valida como audience del id_token.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;
    }
}
