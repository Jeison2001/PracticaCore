using System;

namespace Application.Shared.DTOs.UserInscriptionModality
{
    public class UserInscriptionModalityDto : BaseDto<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdUser { get; set; }
        public string UserName { get; set; }
        public string Identification { get; set; }
        public string Email { get; set; }
        public string CurrentAcademicPeriod { get; set; }
        public double? CumulativeAverage { get; set; }
        public int? ApprovedCredits { get; set; }
        public int? TotalAcademicCredits { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}