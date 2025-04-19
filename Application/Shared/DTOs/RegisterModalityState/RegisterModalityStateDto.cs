using System;

namespace Application.Shared.DTOs.RegisterModalityState
{
    public class RegisterModalityStateDto : BaseDto<long>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSelectable { get; set; } = true;
        public new int? IdUserCreatedAt { get; set; }
    }
}