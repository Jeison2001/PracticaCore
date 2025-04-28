using System;
using System.Threading.Tasks;

namespace Domain.Interfaces.Cache
{
    /// <summary>
    /// Interfaz para el servicio de caché genérico
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Obtiene un valor de la caché. Si no existe, lo carga utilizando la función proporcionada y lo almacena en caché.
        /// </summary>
        /// <typeparam name="T">El tipo de dato a almacenar en caché</typeparam>
        /// <param name="key">La clave única para el valor en caché</param>
        /// <param name="factory">Función para obtener el valor si no está en caché</param>
        /// <param name="absoluteExpirationMinutes">Tiempo en minutos hasta que el valor expire (opcional)</param>
        /// <returns>El valor almacenado en caché o el recién obtenido</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int? absoluteExpirationMinutes = null);

        /// <summary>
        /// Invalida un elemento específico de la caché
        /// </summary>
        /// <param name="key">La clave del elemento a invalidar</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// Invalida todos los elementos de la caché que coinciden con un patrón
        /// </summary>
        /// <param name="pattern">El patrón para las claves a invalidar (ej: "Entity_*")</param>
        Task RemoveByPatternAsync(string pattern);
        
        /// <summary>
        /// Limpia completamente la caché
        /// </summary>
        Task ClearAllAsync();
    }
}