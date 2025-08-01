using Application.Shared.DTOs.TeachingAssignment;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.TeachingAssignment
{
    public class UpdateTeachingAssignmentCommand : IRequest<TeachingAssignmentDto>
    {
        public int Id { get; }
        public TeachingAssignmentDto Dto { get; }
        public UpdateTeachingAssignmentCommand(int id, TeachingAssignmentDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }

    public class UpdateTeachingAssignmentCommandHandler : IRequestHandler<UpdateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationDispatcher _notificationDispatcher;
        private readonly ILogger<UpdateTeachingAssignmentCommandHandler> _logger;

        public UpdateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            INotificationDispatcher notificationDispatcher,
            ILogger<UpdateTeachingAssignmentCommandHandler> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _notificationDispatcher = notificationDispatcher;
            _logger = logger;
        }

        public async Task<TeachingAssignmentDto> Handle(UpdateTeachingAssignmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new InvalidOperationException("Asignación docente no encontrada.");

            // Validar duplicidad
            var exists = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTeacher == dto.IdTeacher &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister &&
                x.Id != request.Id,
                cancellationToken);
            if (exists != null)
                return _mapper.Map<TeachingAssignmentDto>(entity);


            // Mapear cambios
            var originalEntity = _mapper.Map<Domain.Entities.TeachingAssignment>(entity);
            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Procesar notificaciones en background
            ProcessNotificationsAsync(originalEntity, entity);

            return _mapper.Map<TeachingAssignmentDto>(entity);
        }

        private void ProcessNotificationsAsync(Domain.Entities.TeachingAssignment originalEntity, Domain.Entities.TeachingAssignment updatedEntity)
        {
            if (_notificationDispatcher != null)
            {
                // ✅ Fire-and-forget seguro con manejo de scope
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogDebug("Dispatching change notification for TeachingAssignment ID: {AssignmentId}", updatedEntity.Id);
                        await _notificationDispatcher.DispatchEntityChangeAsync<Domain.Entities.TeachingAssignment, int>(originalEntity, updatedEntity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error dispatching change notification for TeachingAssignment ID: {AssignmentId}", updatedEntity.Id);
                    }
                });
            }
        }
    }
}
