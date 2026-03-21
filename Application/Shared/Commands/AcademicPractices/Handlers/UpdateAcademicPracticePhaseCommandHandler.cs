using Domain.Interfaces.Services.Jobs;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.Common.Services.Jobs;

namespace Application.Shared.Commands.AcademicPractices.Handlers
{
    public class UpdateAcademicPracticePhaseCommandHandler : IRequestHandler<UpdateAcademicPracticePhaseCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;

        public UpdateAcademicPracticePhaseCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
        }

        public async Task<bool> Handle(UpdateAcademicPracticePhaseCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var entity = await _academicPracticeRepository.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            // Crear copia del estado original para comparación
            var original = new AcademicPractice { Id = entity.Id, IdStateStage = entity.IdStateStage };

            var result = await _academicPracticeRepository.UpdateAcademicPracticeStateAsync(
                dto.Id,
                dto.NewStateStageId,
                dto.Observations,
                dto.EvaluatorObservations,
                cancellationToken);

            if (!result) return false;

            await _unitOfWork.CommitAsync(cancellationToken);

            // Verificar si realmente hubo cambio de estado y procesar notificación en background
            if (original.IdStateStage != dto.NewStateStageId)
            {
                ProcessNotificationsAsync(dto.Id, original.IdStateStage);
            }

            return true;
        }

        private void ProcessNotificationsAsync(int practiceId, int oldStateId)
        {
            // Fire-and-forget seguro usando Hangfire
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                x => x.HandleAcademicPracticeChangeAsync(practiceId, oldStateId));
        }
    }
}
