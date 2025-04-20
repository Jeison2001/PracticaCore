using Domain.Interfaces.Registration;
using System.Collections.Generic;
using System.Security.Claims;

namespace Domain.Interfaces.Auth
{
    public interface IJwtService : IScopedService
    {
        string GenerateToken(string userId, string email, IEnumerable<string> roles);
        ClaimsPrincipal? ValidateToken(string token);
        void RevokeToken(string token);
        bool IsTokenRevoked(string token);
    }
}