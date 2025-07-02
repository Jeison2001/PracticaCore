using Application.Shared.DTOs.AcademicPractice;
using MediatR;

namespace Application.Shared.Commands.AcademicPractice
{
    public class UpdateAcademicPracticePhaseCommand : IRequest<bool>
    {
        public UpdatePhaseApprovalDto Dto { get; }

        public UpdateAcademicPracticePhaseCommand(UpdatePhaseApprovalDto dto)
        {
            Dto = dto;
        }
    }
}
