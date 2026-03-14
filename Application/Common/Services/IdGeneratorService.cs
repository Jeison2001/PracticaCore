using Domain.Interfaces.Services;

namespace Application.Common.Services
{
    /// <summary>
    /// Implementación del servicio de generación de IDs con ciclo de vida transitorio
    /// Se auto-registrará debido a la interfaz ITransientService
    /// </summary>
    public class IdGeneratorService : IIdGeneratorService
    {
        public string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public Guid GenerateGuid()
        {
            return Guid.NewGuid();
        }
    }
}