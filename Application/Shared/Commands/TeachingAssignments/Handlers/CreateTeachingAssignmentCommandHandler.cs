using Application.Common.Services;
using Application.Common.Services.Jobs;
using Application.Shared.DTOs.TeachingAssignments;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.TeachingAssignments.Handlers
{
    /// <summary>
    /// Crea una asignación docente tras validar que el docente tiene habilitado el cargo
    /// (TeacherEnabledCargo) y el límite de MaxAssignments para la combinación docente/cargo.
    /// Maneja race condition: si ocurre una violación de constraint única (23505),
    /// retorna la asignación existente en lugar de fallar — previene notificaciones duplicadas
    /// cuando dos solicitudes compiten. Encola HandleTeachingAssignmentCreationAsync tras creación.
    /// </summary>
    public class CreateTeachingAssignmentCommandHandler : IRequestHandler<CreateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IRepository<TypeTeachingAssignment, int> _typeRepository;
        private readonly IRepository<TeacherEnabledCargo, int> _teacherEnabledCargoRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<CreateTeachingAssignmentCommandHandler> _logger;

        public CreateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IRepository<TypeTeachingAssignment, int> typeRepository,
            IRepository<TeacherEnabledCargo, int> teacherEnabledCargoRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer,
            ILogger<CreateTeachingAssignmentCommandHandler> logger)
        {
            _repository = repository;
            _typeRepository = typeRepository;
            _teacherEnabledCargoRepository = teacherEnabledCargoRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<TeachingAssignmentDto> Handle(CreateTeachingAssignmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            // Obtener el tipo de asignación para conocer el código y límite
            var type = await _typeRepository.GetByIdAsync(dto.IdTypeTeachingAssignment);
            if (type == null)
                throw new InvalidOperationException("Tipo de asignación docente no encontrado.");

            if (!dto.IsConfirmed)
            {
                var hasOfficialPermission =
                    request.CurrentUser.Permissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPA) ||
                    request.CurrentUser.Permissions.Contains(PermissionCodes.ProyectoGrado.N4PGPAA) ||
                    request.CurrentUser.Permissions.Contains(PermissionCodes.PracticaAcademica.N4PAF0PA);

                if (request.CurrentUser.UserId.HasValue && !hasOfficialPermission)
                {
                    var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                    var isActiveMember = await uimRepo.AnyAsync(
                        x => x.IdInscriptionModality == dto.IdInscriptionModality &&
                             x.IdUser == request.CurrentUser.UserId.Value &&
                             x.StatusRegister,
                        cancellationToken);

                    if (!isActiveMember)
                        throw new UnauthorizedAccessException("Solo los estudiantes activos de la inscripción pueden sugerir docentes.");
                }

                if (string.Equals(type.Code, TeachingAssignmentEvaluationHelper.JuradoEvaluadorCode, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("No está permitido preseleccionar un Jurado Evaluador.");
            }

            // Validar que el docente tiene habilitado este cargo (TeacherEnabledCargo)
            var cargoHabilitado = await _teacherEnabledCargoRepository.AnyAsync(
                x => x.IdUser == dto.IdTeacher &&
                     x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                     x.StatusRegister,
                cancellationToken);
            if (!cargoHabilitado)
                throw new InvalidOperationException($"El docente no tiene habilitado el cargo '{type.Name}' para asignaciones docentes.");

            // Verificar si ya existe una asignación activa para el mismo teacher, inscripcion y cargo
            var existing = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdTeacher == dto.IdTeacher &&
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister,
                cancellationToken);
            if (existing != null)
            {
                if (!existing.IsConfirmed && dto.IsConfirmed)
                {
                    await PromoteUnconfirmedAssignmentAsync(existing, type, dto, request.CurrentUser.UserId, cancellationToken);
                }

                return _mapper.Map<TeachingAssignmentDto>(existing);
            }

            if (dto.IsConfirmed)
            {
                await TeachingAssignmentEvaluationHelper.ValidateMaxAssignmentsAsync(
                    _repository,
                    type,
                    dto.IdTeacher,
                    excludeAssignmentId: null,
                    cancellationToken);
            }

            // Crear nueva asignación directamente
            var entity = _mapper.Map<TeachingAssignment>(dto);
            entity.IsConfirmed = dto.IsConfirmed;
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
                        if (!ReferenceEquals(raceConditionExisting, entity))
                            await _repository.DeleteAsync(entity);

                        if (!raceConditionExisting.IsConfirmed && dto.IsConfirmed)
                        {
                            await PromoteUnconfirmedAssignmentAsync(raceConditionExisting, type, dto, request.CurrentUser.UserId, cancellationToken);
                        }

                        return _mapper.Map<TeachingAssignmentDto>(raceConditionExisting);
                    }
                }
                throw; // Lanzar la excepción si no es una carrera de concurrencia esperada
            }

            if (entity.IsConfirmed)
            {
                // Procesar notificaciones en background
                ProcessNotificationsAsync(entity);

                if (string.Equals(type.Code, TeachingAssignmentEvaluationHelper.JuradoEvaluadorCode, StringComparison.OrdinalIgnoreCase) &&
                    entity.StatusRegister &&
                    entity.RevocationDate == null)
                {
                    await TeachingAssignmentEvaluationHelper.TryAdvancePreliminaryProjectToEvaluationAsync(
                        _unitOfWork,
                        _repository,
                        entity.IdInscriptionModality,
                        type.Id,
                        request.CurrentUser.UserId,
                        cancellationToken);
                }
            }

            return _mapper.Map<TeachingAssignmentDto>(entity);
        }

        private async Task PromoteUnconfirmedAssignmentAsync(
            TeachingAssignment existing,
            TypeTeachingAssignment type,
            TeachingAssignmentDto dto,
            int? userId,
            CancellationToken cancellationToken)
        {
            await TeachingAssignmentEvaluationHelper.ValidateMaxAssignmentsAsync(
                _repository,
                type,
                dto.IdTeacher,
                excludeAssignmentId: existing.Id,
                cancellationToken);

            existing.IsConfirmed = true;
            existing.OperationRegister = dto.OperationRegister ?? existing.OperationRegister;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.IdUserUpdatedAt = userId;

            await _repository.UpdateAsync(existing);
            await _unitOfWork.CommitAsync(cancellationToken);

            ProcessNotificationsAsync(existing);

            if (string.Equals(type.Code, TeachingAssignmentEvaluationHelper.JuradoEvaluadorCode, StringComparison.OrdinalIgnoreCase) &&
                existing.RevocationDate == null)
            {
                await TeachingAssignmentEvaluationHelper.TryAdvancePreliminaryProjectToEvaluationAsync(
                    _unitOfWork,
                    _repository,
                    existing.IdInscriptionModality,
                    type.Id,
                    userId,
                    cancellationToken);
            }
        }

        private void ProcessNotificationsAsync(TeachingAssignment entity)
        {
            // Fire-and-forget seguro usando Hangfire mediante dispatcher genérico envoltorio
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleTeachingAssignmentCreationAsync(entity.Id));
        }
    }
}
