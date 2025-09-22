using Application.Shared.DTOs.AcademicPractice;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using MediatR;

namespace Application.Shared.Commands.AcademicPractice.Handlers
{
    public class UpdateAcademicPracticePhaseCommandHandler : IRequestHandler<UpdateAcademicPracticePhaseCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _notificationDispatcher;

        public UpdateAcademicPracticePhaseCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork,
            INotificationDispatcher notificationDispatcher)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task<bool> Handle(UpdateAcademicPracticePhaseCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var entity = await _academicPracticeRepository.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            // Crear copia del estado original para comparación
            var original = new Domain.Entities.AcademicPractice { Id = entity.Id, IdStateStage = entity.IdStateStage };

            var result = await _academicPracticeRepository.UpdateAcademicPracticeStateAsync(
                dto.Id,
                dto.NewStateStageId,
                dto.Observations,
                dto.EvaluatorObservations,
                cancellationToken);

            if (!result) return false;

            await _unitOfWork.CommitAsync(cancellationToken);

            // Verificar si realmente hubo cambio de estado y disparar notificación
            if (original.IdStateStage != dto.NewStateStageId)
            {
                // Obtener la entidad actualizada para la notificación
                var updatedEntity = await _academicPracticeRepository.GetByIdAsync(dto.Id);
                if (updatedEntity != null)
                {
                    await _notificationDispatcher.DispatchEntityChangeAsync<Domain.Entities.AcademicPractice, int>(original, updatedEntity, cancellationToken);
                }
            }

            return true;
        }
    }
}
