using Domain.Interfaces.Notifications;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions
{
    /// <summary>
    /// Extensión para registrar servicios de notificaciones por email
    /// </summary>
    public static class EmailNotificationServiceExtensions
    {
        public static IServiceCollection AddEmailNotificationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Determinar el proveedor configurado
            var provider = configuration["EmailNotification:Provider"]?.ToUpper() ?? "SMTP";

            // Validar proveedor soportado
            if (provider != "SMTP")
            {
                throw new NotSupportedException($"Proveedor de notificaciones '{provider}' no soportado. Solo SMTP está disponible actualmente.");
            }

            // Registrar el servicio SMTP
            services.AddScoped<INotificationService, SmtpNotificationService>();

            // Nota: Las implementaciones deben leer SIEMPRE de EmailNotification:SmtpSettings
            // Ejemplo: configuration.GetSection("EmailNotification:SmtpSettings")

            return services;
        }
    }
}
