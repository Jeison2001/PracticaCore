using Application.Shared.DTOs.AcademicPractices;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.AcademicPractices
{
    public record UpdateAcademicPracticeInstitutionCommand : IRequest<bool>
    {
        public UpdateInstitutionInfoDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }

        public UpdateAcademicPracticeInstitutionCommand(UpdateInstitutionInfoDto dto, CurrentUserInfo currentUser)
        {
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}
