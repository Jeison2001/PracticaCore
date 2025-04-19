using System;

namespace Application.Shared.DTOs.RegisterModalityStudent
{
    public class RegisterModalityStudentDto : BaseDto<int>
    {
        public int IdRegisterModality { get; set; }
        public int IdUser { get; set; }
        public string UserName { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }
    }
}