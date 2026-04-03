using Application.Shared.DTOs.AcademicPractices;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.AcademicPractices
{
    public record UpdateAcademicPracticePhaseCommand : IRequest<bool>
    {
        public UpdatePhaseApprovalDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }

        public UpdateAcademicPracticePhaseCommand(UpdatePhaseApprovalDto dto, CurrentUserInfo currentUser)
        {
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}
