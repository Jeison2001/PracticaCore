using Application.Shared.Commands.AcademicPractice;
using Application.Shared.DTOs.AcademicPractice;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands.AcademicPractice.Handlers
{
    public class UpdateAcademicPracticePhaseCommandHandler : IRequestHandler<UpdateAcademicPracticePhaseCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAcademicPracticePhaseCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateAcademicPracticePhaseCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var result = await _academicPracticeRepository.UpdateAcademicPracticeStateAsync(
                dto.Id, // id
                dto.NewStateStageId, // nuevo estado de la fase
                dto.Observations,
                dto.EvaluatorObservations,
                cancellationToken);

            if (result)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            return result;
        }
    }
}
