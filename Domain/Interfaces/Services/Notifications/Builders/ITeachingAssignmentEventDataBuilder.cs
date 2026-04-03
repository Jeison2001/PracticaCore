using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Construye placeholders para emails de TeachingAssignment: docente, tipo de
    /// asignación, título del proyecto (desde Proposal o AcademicPractice),
    /// estudiantes, fase actual y período académico.
    /// </summary>
    public interface ITeachingAssignmentEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildTeachingAssignmentEventDataAsync(int assignmentId, string eventType);
    }
}
