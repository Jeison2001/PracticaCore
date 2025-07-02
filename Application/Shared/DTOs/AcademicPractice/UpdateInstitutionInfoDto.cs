using Application.Shared.DTOs;

namespace Application.Shared.DTOs.AcademicPractice
{
    public class UpdateInstitutionInfoDto : BaseDto<int>
    {
        public string? Title { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTime? PracticeStartDate { get; set; }
        public DateTime? PracticeEndDate { get; set; }
        public int? PracticeHours { get; set; }
        public bool IsEmprendimiento { get; set; }
        public string? Observations { get; set; }
    }
}
