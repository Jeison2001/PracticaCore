using Domain.Interfaces.Registration;
using System.Collections.Generic;
using System.Security.Claims;

namespace Domain.Interfaces.Auth
{
    public interface IJwtService : IScopedService
    {
        ClaimsPrincipal? ValidateToken(string token);
        void RevokeToken(string token);
        bool IsTokenRevoked(string token);
        public string GenerateTokenWithClaims(
            string userId, 
            string email, 
            IEnumerable<string> roles, 
            IEnumerable<string> permissions = null,
            string firstName = null,
            string lastName  = null,
            string identification = null);
    }
}