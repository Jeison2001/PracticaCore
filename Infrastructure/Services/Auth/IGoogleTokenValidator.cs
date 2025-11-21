using Domain.Interfaces.Common;
using Google.Apis.Auth;

namespace Infrastructure.Services.Auth
{
    public interface IGoogleTokenValidator : IScopedService
    {
        Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
    }
}
