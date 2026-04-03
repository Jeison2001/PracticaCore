using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Infrastructure.Services.Auth
{
    /// <summary>
    /// Servicio de Refresh Tokens con soporte para rotación de tokens.
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        private const int TokenExpirationDays = 7;
        private const int TokenByteLength = 64; // 512 bits → 86 chars en Base64

        public RefreshTokenService(AppDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        /// <inheritdoc />
        public async Task<RefreshToken> GenerateAsync(int userId, CancellationToken ct = default)
        {
            var tokenValue = GenerateSecureToken();

            var refreshToken = new RefreshToken
            {
                IdUser = userId,
                Token = tokenValue,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(TokenExpirationDays),
                CreatedAt = DateTimeOffset.UtcNow,
                StatusRegister = true,
                OperationRegister = "Generación Token"
            };

            await _context.RefreshTokens.AddAsync(refreshToken, ct);
            await _unitOfWork.CommitAsync(ct);

            return refreshToken;
        }

        /// <inheritdoc />
        public async Task<RefreshToken?> ValidateAsync(string token, CancellationToken ct = default)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, ct);

            if (refreshToken == null || !refreshToken.IsActive)
                return null;

            return refreshToken;
        }

        /// <inheritdoc />
        public async Task RevokeAsync(string token, CancellationToken ct = default)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, ct);

            if (refreshToken == null || refreshToken.RevokedAt.HasValue)
                return; // Ya revocado o no existe — no lanzar excepción

            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            refreshToken.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.CommitAsync(ct);
        }

        /// <inheritdoc />
        public async Task PurgeExpiredAsync(int userId, CancellationToken ct = default)
        {
            var stale = await _context.RefreshTokens
                .Where(rt => rt.IdUser == userId && (rt.RevokedAt != null || rt.ExpiresAt <= DateTimeOffset.UtcNow))
                .ToListAsync(ct);

            if (stale.Any())
            {
                _context.RefreshTokens.RemoveRange(stale);
                await _unitOfWork.CommitAsync(ct);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
            return Convert.ToBase64String(bytes);
        }
    }
}
