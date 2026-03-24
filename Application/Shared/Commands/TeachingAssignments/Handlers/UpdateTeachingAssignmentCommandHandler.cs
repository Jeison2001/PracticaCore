using Application.Shared.DTOs.TeachingAssignments;
using AutoMapper;
using Domain.Interfaces.Services.Notifications.Dispatcher;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using Application.Common.Services.Jobs;

namespace Application.Shared.Commands.TeachingAssignments.Handlers
{
    public class UpdateTeachingAssignmentCommandHandler : IRequestHandler<UpdateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<UpdateTeachingAssignmentCommandHandler> _logger;

        public UpdateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer,
            ILogger<UpdateTeachingAssignmentCommandHandler> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
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

            // Capturar solo el valor necesario para notificaciones ANTES de modificar
            var originalTeacherId = entity.IdTeacher;

            // Preservar campos de auditoría inmutables
            var originalCreatedAt = entity.CreatedAt;
            var originalIdUserCreatedAt = entity.IdUserCreatedAt;

            // Mapear cambios
            _mapper.Map(dto, entity);

            // Restaurar campos inmutables y asegurar UTC
            entity.CreatedAt = originalCreatedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(originalCreatedAt, DateTimeKind.Utc)
                : originalCreatedAt;
            entity.IdUserCreatedAt = originalIdUserCreatedAt;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Procesar notificaciones en background
            ProcessNotificationsAsync(entity, originalTeacherId);

            return _mapper.Map<TeachingAssignmentDto>(entity);
        }

        private void ProcessNotificationsAsync(TeachingAssignment updatedEntity, int originalTeacherId)
        {
            // ✅ Fire-and-forget seguro usando Hangfire
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentChangeAsync(updatedEntity.Id, originalTeacherId));
        }
    }
}
