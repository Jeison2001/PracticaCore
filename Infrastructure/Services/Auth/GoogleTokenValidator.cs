using Google.Apis.Auth;

namespace Infrastructure.Services.Auth
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken)
        {
            return await GoogleJsonWebSignature.ValidateAsync(idToken);
        }
    }
}
