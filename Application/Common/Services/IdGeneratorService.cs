using Domain.Interfaces.Services;

namespace Application.Common.Services
{
    /// <summary>
    /// Genera identificadores únicos: GUID sin guiones (N) o GUID completo.
    /// Usado por el sistema de file storage para nombres de archivo.
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