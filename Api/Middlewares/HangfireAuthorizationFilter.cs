using Hangfire.Dashboard;
using System.Text;

namespace Api.Middlewares
{
    /// <summary>
    /// Filtro de autorización para el Dashboard de Hangfire.
    /// Development: acceso libre. Producción: Basic Auth.
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly string _user;
        private readonly string _password;
        private readonly bool _isDevelopment;

        public HangfireAuthorizationFilter(IConfiguration configuration, IWebHostEnvironment env)
        {
            _user = configuration["Hangfire:Dashboard:User"]
                ?? throw new InvalidOperationException("Hangfire:Dashboard:User no está configurado en appsettings.");
            _password = configuration["Hangfire:Dashboard:Password"]
                ?? throw new InvalidOperationException("Hangfire:Dashboard:Password no está configurado en appsettings.");
            _isDevelopment = env.IsDevelopment();
        }

        public bool Authorize(DashboardContext context)
        {
            // En Development: acceso libre (corre solo en la máquina del desarrollador)
            if (_isDevelopment)
                return true;

            // Producción/Staging: requiere Basic Auth
            var httpContext = context.GetHttpContext();

            // Forzar HTTPS en producción para que las credenciales viajen cifradas
            if (!httpContext.Request.IsHttps)
            {
                httpContext.Response.Redirect(
                    $"https://{httpContext.Request.Host}{httpContext.Request.Path}");
                return false;
            }

            var header = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                Challenge(httpContext.Response);
                return false;
            }

            try
            {
                var encoded = header["Basic ".Length..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var parts = decoded.Split(':', 2);

                // Comparación constante en tiempo para evitar timing attacks
                if (parts.Length == 2
                    && CryptographicEquals(parts[0], _user)
                    && CryptographicEquals(parts[1], _password))
                    return true;
            }
            catch
            {
                // Credenciales mal formadas
            }

            Challenge(httpContext.Response);
            return false;
        }

        /// <summary>
        /// Comparación de strings en tiempo constante para evitar timing attacks.
        /// </summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }

        private static void Challenge(HttpResponse response)
        {
            response.StatusCode = 401;
            response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
        }
    }
}
