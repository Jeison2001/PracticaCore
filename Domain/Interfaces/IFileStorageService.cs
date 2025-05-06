using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda un archivo y retorna el nombre o ruta única generada.
        /// </summary>
        Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);

        /// <summary>
        /// Obtiene un archivo como stream a partir de su nombre/ruta.
        /// </summary>
        Task<Stream> GetFileAsync(string fileName, CancellationToken cancellationToken);

        /// <summary>
        /// Elimina un archivo por su nombre/ruta.
        /// </summary>
        Task DeleteFileAsync(string fileName, CancellationToken cancellationToken);
    }
}