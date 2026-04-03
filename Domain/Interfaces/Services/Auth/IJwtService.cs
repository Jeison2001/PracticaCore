using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Auth
{
    public interface IJwtService : IScopedService
    {
        string GenerateTokenWithClaims(
            string userId, 
            string email, 
            IEnumerable<string> roles, 
            IEnumerable<string>? permissions = null,
            string? firstName = null,
            string? lastName  = null,
            string? identification = null);
    }
}