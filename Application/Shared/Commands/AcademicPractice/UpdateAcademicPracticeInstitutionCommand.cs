using MediatR;

namespace Application.Shared.Commands.AcademicPractice
{
    public class UpdateAcademicPracticeInstitutionCommand : IRequest<bool>
    {
        public int AcademicPracticeId { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTime? PracticeStartDate { get; set; }
        public DateTime? PracticeEndDate { get; set; }
        public int? PracticeHours { get; set; }
        public bool? IsEmprendimiento { get; set; }
        public string? Observations { get; set; }
        public int UserId { get; set; }
    }
}
