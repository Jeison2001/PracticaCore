using System;

namespace Application.Shared.DTOs.RegisterModality
{
    public class RegisterModalityDto : BaseDto<int>
    {
        public int IdModality { get; set; }
        public int IdRegisterModalityState { get; set; }
        public int IdAcademicPeriod { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}