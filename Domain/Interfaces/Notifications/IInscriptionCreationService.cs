using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Servicio específico para manejar la creación de inscripciones con estudiantes.
    /// Interface para mejorar testabilidad y adherencia a Dependency Inversion Principle.
    /// </summary>
    public interface IInscriptionCreationService : IScopedService
    {
        Task ProcessInscriptionCreationAsync(int inscriptionId, int modalityId, int academicPeriodId, IEnumerable<int> studentIds, CancellationToken cancellationToken = default);
    }
}
