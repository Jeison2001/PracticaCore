using Domain.Interfaces.Registration;
using System.Threading.Tasks;

namespace Domain.Interfaces.Auth
{
    public interface IAuthService : IScopedService
    {
        /// <summary>
        /// Autentica a un usuario usando un token de Google
        /// </summary>
        /// <param name="idToken">El token ID de Google</param>
        /// <returns>Resultado de la autenticación como objeto dinámico</returns>
        Task<dynamic> AuthenticateWithGoogleAsync(string idToken);
    }
}