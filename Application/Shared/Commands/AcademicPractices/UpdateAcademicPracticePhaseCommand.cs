using Application.Shared.DTOs.AcademicPractices;
using MediatR;

namespace Application.Shared.Commands.AcademicPractices
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
