using Domain.Entities;
using Domain.Interfaces.Registration;

namespace Domain.Interfaces
{
    public interface IAcademicPeriodService : IScopedService
    {
        /// <summary>
        /// Auto-detecta el período académico activo basado en la fecha actual
        /// </summary>
        /// <returns>El período académico activo o null si no hay ninguno activo</returns>
        Task<AcademicPeriod?> GetActiveAcademicPeriodAsync();
        
        /// <summary>
        /// Obtiene un período académico por su ID
        /// </summary>
        /// <param name="id">ID del período académico</param>
        /// <returns>El período académico o null si no existe</returns>
        Task<AcademicPeriod?> GetAcademicPeriodByIdAsync(int id);
    }
}
