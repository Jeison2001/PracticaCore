namespace Domain.Configuration
{
    /// <summary>
    /// Opciones de configuración para autenticación con Google Sign-In.
    /// </summary>
    public class GoogleAuthOptions
    {
        /// <summary>
        /// ClientId de la aplicación OAuth de Google. Se valida como audience del id_token.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Dominio hospedado opcional (Google Workspace) al que debe pertenecer el usuario.
        /// </summary>
        public string? HostedDomain { get; set; }
    }
}
