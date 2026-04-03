using System.Linq.Expressions;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Jobs
{
    /// <summary>
    /// Puente entre comandos C# y Hangfire. Encola jobs asíncronos (notificaciones,
    /// etc.) usando expression trees. El tipo T es NotificationBackgroundJob.
    /// Retorna el jobId de Hangfire.
    /// </summary>
    public interface IJobEnqueuer : ITransientService
    {
        string Enqueue<T>(Expression<Action<T>> methodCall);
    }
}
