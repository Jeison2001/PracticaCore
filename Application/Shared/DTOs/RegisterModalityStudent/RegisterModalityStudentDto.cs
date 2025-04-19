using System;

namespace Application.Shared.DTOs.RegisterModalityStudent
{
    public class RegisterModalityStudentDto : BaseDto<long>
    {
        public long IdRegisterModality { get; set; }
        public int IdUser { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}