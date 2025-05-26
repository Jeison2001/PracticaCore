using System;

namespace Application.Shared.DTOs.UserInscriptionModality
{
    public class UserInscriptionModalityDto : BaseDto<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdUser { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public int IdIdentificationType { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CurrentAcademicPeriod { get; set; } = string.Empty;
        public double? CumulativeAverage { get; set; }
        public int? ApprovedCredits { get; set; }
        public int? TotalAcademicCredits { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}