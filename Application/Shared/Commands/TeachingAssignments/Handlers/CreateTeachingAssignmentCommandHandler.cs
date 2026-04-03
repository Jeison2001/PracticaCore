using Application.Shared.DTOs.TeachingAssignments;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Dispatcher;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using Application.Common.Services.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Application.Shared.Commands.TeachingAssignments.Handlers
{
    /// <summary>
    /// Crea una asignación docente tras validar el límite de MaxAssignments para la combinación
    /// docente/cargo. Maneja race condition: si ocurre una violación de constraint única (23505),
    /// retorna la asignación existente en lugar de fallar — previene notificaciones duplicadas
    /// cuando dos solicitudes compiten. Encola HandleTeachingAssignmentCreationAsync tras creación.
    /// </summary>
    public class CreateTeachingAssignmentCommandHandler : IRequestHandler<CreateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IRepository<TypeTeachingAssignment, int> _typeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<CreateTeachingAssignmentCommandHandler> _logger;

        public CreateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IRepository<TypeTeachingAssignment, int> typeRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer,
            ILogger<CreateTeachingAssignmentCommandHandler> logger)
        {
            _repository = repository;
            _typeRepository = typeRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
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
                var count = await _repository.CountAsync(x => x.IdTeacher == dto.IdTeacher && x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment && x.StatusRegister, cancellationToken);
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
            entity.IdUserCreatedAt = request.CurrentUser.UserId;
            
            try
            {
                await _repository.AddAsync(entity);
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Extraer el SqlState de forma libre de dependencias (para no acoplar Application con Npgsql)
                var innerExceptionType = ex.InnerException?.GetType().Name;
                var sqlStateProperty = ex.InnerException?.GetType().GetProperty("SqlState");
                var sqlState = sqlStateProperty?.GetValue(ex.InnerException) as string;

                if (innerExceptionType == "PostgresException" && sqlState == "23505")
                {
                    _logger.LogWarning("Condición de carrera prevenida: El docente {IdTeacher} ya fue asignado activamente en la inscripción {IdInscriptionModality} con el cargo {IdTypeTeachingAssignment}.", dto.IdTeacher, dto.IdInscriptionModality, dto.IdTypeTeachingAssignment);
                    
                    var raceConditionExisting = await _repository.GetFirstOrDefaultAsync(x =>
                        x.IdTeacher == dto.IdTeacher &&
                        x.IdInscriptionModality == dto.IdInscriptionModality &&
                        x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                        x.StatusRegister,
                        cancellationToken);

                    if (raceConditionExisting != null)
                    {
                        return _mapper.Map<TeachingAssignmentDto>(raceConditionExisting);
                    }
                }
                throw; // Lanzar la excepción si no es una carrera de concurrencia esperada
            }

            // Procesar notificaciones en background
            ProcessNotificationsAsync(entity);

            return _mapper.Map<TeachingAssignmentDto>(entity);
        }

        private void ProcessNotificationsAsync(TeachingAssignment entity)
        {
            // Fire-and-forget seguro usando Hangfire mediante dispatcher genérico envoltorio
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentCreationAsync(entity.Id));
        }
    }
}
