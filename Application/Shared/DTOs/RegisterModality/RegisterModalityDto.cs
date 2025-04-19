using System;

namespace Application.Shared.DTOs.RegisterModality
{
    public class RegisterModalityDto : BaseDto<long>
    {
        public long IdModality { get; set; }
        public long IdRegisterModalityState { get; set; }
        public long IdAcademicPeriod { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}