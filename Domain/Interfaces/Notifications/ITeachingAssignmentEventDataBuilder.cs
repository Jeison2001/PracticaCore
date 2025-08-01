using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Builder específico para eventos de TeachingAssignment.
    /// Single Responsibility: Solo construye datos para eventos de asignaciones docentes.
    /// </summary>
    public interface ITeachingAssignmentEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildTeachingAssignmentEventDataAsync(int assignmentId, string eventType);
    }
}
