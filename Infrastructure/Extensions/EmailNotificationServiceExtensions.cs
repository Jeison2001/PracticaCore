using Application.Common.Services;
using Domain.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            
            // Solo registrar el servicio de notificación básico apropiado
            // Los demás servicios (IEmailNotificationEventService, IEmailRecipientResolverService) 
            // se auto-registran con Scrutor por heredar de IScopedService
            switch (provider)
            {
                case "GOOGLE":
                case "GMAIL":
                    services.AddScoped<INotificationService, GoogleNotificationService>();
                    break;
                    
                case "AZURE":
                case "OUTLOOK":
                    services.AddScoped<INotificationService, AzureNotificationService>();
                    break;
                    
                case "SMTP":
                default:
                    services.AddScoped<INotificationService, SmtpNotificationService>();
                    break;
            }

            return services;
        }
    }
}
