using Application.Shared.DTOs.AcademicPractice;
using MediatR;

namespace Application.Shared.Commands.AcademicPractice
{
    public class UpdateAcademicPracticeInstitutionCommand : IRequest<bool>
    {
        public UpdateInstitutionInfoDto Dto { get; }

        public UpdateAcademicPracticeInstitutionCommand(UpdateInstitutionInfoDto dto)
        {
            Dto = dto;
        }
    }
}
