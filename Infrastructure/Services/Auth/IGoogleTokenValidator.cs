using Domain.Interfaces.Registration;
using Google.Apis.Auth;

namespace Infrastructure.Services.Auth
{
    public interface IGoogleTokenValidator : IScopedService
    {
        Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
    }
}
