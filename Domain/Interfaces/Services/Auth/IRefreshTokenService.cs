using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Auth
{
    public interface IRefreshTokenService : IScopedService
    {
        /// <summary>Genera un nuevo Refresh Token para el usuario.</summary>
        Task<RefreshToken> GenerateAsync(int userId, CancellationToken ct = default);

        /// <summary>Valida el token. Retorna null si expiró o fue revocado.</summary>
        Task<RefreshToken?> ValidateAsync(string token, CancellationToken ct = default);

        /// <summary>Revoca el token. No lanza excepción si ya estaba revocado.</summary>
        Task RevokeAsync(string token, CancellationToken ct = default);

        /// <summary>Valida un token permitiendo saber si existe aunque esté revocado (Para Token Family Revocation).</summary>
        Task<RefreshToken?> ValidateTokenForReuseAsync(string token, CancellationToken ct = default);

        /// <summary>Borra todas las sesiones del usuario de forma inmediata como mecanismo de defensa.</summary>
        Task RevokeAllTokensForUserAsync(int userId, CancellationToken ct = default);

        /// <summary>Elimina tokens expirados o revocados del usuario.</summary>
        Task PurgeExpiredAsync(int userId, CancellationToken ct = default);
    }
}
