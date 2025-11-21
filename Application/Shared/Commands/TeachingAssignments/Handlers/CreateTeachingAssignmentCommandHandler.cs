using Application.Shared.DTOs.TeachingAssignments;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Dispatcher;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Commands.TeachingAssignments.Handlers
{
    public class CreateTeachingAssignmentCommandHandler : IRequestHandler<CreateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IRepository<TypeTeachingAssignment, int> _typeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationDispatcher _notificationDispatcher;
        private readonly ILogger<CreateTeachingAssignmentCommandHandler> _logger;

        public CreateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IRepository<TypeTeachingAssignment, int> typeRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            INotificationDispatcher notificationDispatcher,
            ILogger<CreateTeachingAssignmentCommandHandler> logger)
        {
            _repository = repository;
            _typeRepository = typeRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _notificationDispatcher = notificationDispatcher;
            _logger = logger;
        }

        public async Task<TeachingAssignmentDto> Handle(CreateTeachingAssignmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            // Obtener el tipo de asignación para conocer el límite
            var type = await _typeRepository.GetByIdAsync(dto.IdTypeTeachingAssignment);
            if (type == null)
                throw new InvalidOperationException("Tipo de asignación docente no encontrado.");

            if (type.MaxAssignments.HasValue)
            {
                // Contar asignaciones activas para este docente y cargo
                var assignments = await _repository.GetAllAsync(x => x.IdTeacher == dto.IdTeacher && x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment && x.StatusRegister);
                var count = assignments.Count();
                if (count >= type.MaxAssignments.Value)
                    throw new InvalidOperationException($"El docente ya tiene el máximo permitido ({type.MaxAssignments}) de asignaciones activas para este cargo.");
            }

            // Verificar si ya existe una asignación activa para el mismo teacher, inscripcion y cargo
            var existing = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdTeacher == dto.IdTeacher &&
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister,
                cancellationToken);
            if (existing != null)
            {
                // Ya existe exactamente la misma asignación activa, retornar la existente
                return _mapper.Map<TeachingAssignmentDto>(existing);
            }

            // Crear nueva asignación directamente
            var entity = _mapper.Map<TeachingAssignment>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Procesar notificaciones en background
            ProcessNotificationsAsync(entity);

            return _mapper.Map<TeachingAssignmentDto>(entity);
        }

        private void ProcessNotificationsAsync(TeachingAssignment entity)
        {
            if (_notificationDispatcher != null)
            {
                // ✅ Fire-and-forget seguro con manejo de scope
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogDebug("Dispatching creation notification for TeachingAssignment ID: {AssignmentId}", entity.Id);
                        await _notificationDispatcher.DispatchEntityCreationAsync<TeachingAssignment, int>(entity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error dispatching creation notification for TeachingAssignment ID: {AssignmentId}", entity.Id);
                    }
                });
            }
        }
    }
}
