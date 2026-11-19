using System.Text.Json.Serialization;

namespace Application.Shared.DTOs.Auth
{
    public record AuthResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }
        public UserInfoDto User { get; set; } = new UserInfoDto();
        public List<AuthRoleDto> Roles { get; set; } = new List<AuthRoleDto>();
        public Dictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();
    }
}
