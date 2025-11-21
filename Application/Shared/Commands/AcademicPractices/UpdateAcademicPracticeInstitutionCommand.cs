using Application.Shared.DTOs.AcademicPractices;
using MediatR;

namespace Application.Shared.Commands.AcademicPractices
{
    public record UpdateAcademicPracticeInstitutionCommand : IRequest<bool>
    {
        public UpdateInstitutionInfoDto Dto { get; }

        public UpdateAcademicPracticeInstitutionCommand(UpdateInstitutionInfoDto dto)
        {
            Dto = dto;
        }
    }
}
