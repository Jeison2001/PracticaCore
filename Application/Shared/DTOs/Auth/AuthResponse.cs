using System.Collections.Generic;

namespace Application.Shared.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserInfoDto User { get; set; } = new UserInfoDto();
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
    }
}