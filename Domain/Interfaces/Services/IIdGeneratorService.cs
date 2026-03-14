using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services
{
    /// <summary>
    /// Servicio para generar identificadores únicos
    /// </summary>
    public interface IIdGeneratorService : ITransientService
    {
        string GenerateUniqueId();
        Guid GenerateGuid();
    }
}
