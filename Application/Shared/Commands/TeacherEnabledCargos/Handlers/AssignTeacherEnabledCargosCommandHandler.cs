using Application.Shared.DTOs.TeacherEnabledCargos;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Commands.TeacherEnabledCargos.Handlers
{
    /// <summary>
    /// Asigna los cargos habilitados a un docente (upsert semántico): activa los
    /// cargos recibidos y desactiva los que ya no están en la lista.
    /// </summary>
    public class AssignTeacherEnabledCargosCommandHandler : IRequestHandler<AssignTeacherEnabledCargosCommand, List<TeacherEnabledCargoDto>>
    {
        private readonly ITeacherEnabledCargoRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignTeacherEnabledCargosCommandHandler(
            ITeacherEnabledCargoRepository repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TeacherEnabledCargoDto>> Handle(AssignTeacherEnabledCargosCommand request, CancellationToken cancellationToken)
        {
            var requestedIds = request.TypeTeachingAssignmentIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (requestedIds.Count == 0)
            {
                throw new InvalidOperationException("Debe especificar al menos un cargo docente habilitado.");
            }

            var user = await _unitOfWork.GetRepository<User, int>()
                .GetFirstOrDefaultAsync(u => u.Id == request.UserId && u.StatusRegister, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"El usuario con ID {request.UserId} no existe o está inactivo.");
            }

            var teacherRole = await _unitOfWork.GetRepository<Role, int>()
                .GetFirstOrDefaultAsync(r => r.Code == "TEACHER" && r.StatusRegister, cancellationToken);

            var hasActiveTeacherRole = teacherRole != null && await _unitOfWork.GetRepository<UserRole, int>()
                .AnyAsync(ur => ur.IdUser == request.UserId && ur.IdRole == teacherRole.Id && ur.StatusRegister, cancellationToken);

            if (!hasActiveTeacherRole)
            {
                throw new InvalidOperationException("Solo se pueden habilitar cargos docentes a usuarios con el rol activo de Docente (TEACHER).");
            }

            var activeCargoCount = await _unitOfWork.GetRepository<TypeTeachingAssignment, int>()
                .CountAsync(t => requestedIds.Contains(t.Id) && t.StatusRegister, cancellationToken);
            if (activeCargoCount != requestedIds.Count)
            {
                throw new InvalidOperationException("Uno o más cargos docentes especificados no existen o están inactivos.");
            }

            await _repository.BulkAssignAsync(
                request.UserId,
                requestedIds,
                request.CurrentUser.UserId,
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            var result = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

            return result.Select(c => new TeacherEnabledCargoDto
            {
                Id = c.Id,
                IdUser = c.IdUser,
                IdTypeTeachingAssignment = c.IdTypeTeachingAssignment,
                CargoCode = c.TypeTeachingAssignment?.Code,
                CargoName = c.TypeTeachingAssignment?.Name,
                MaxAssignments = c.TypeTeachingAssignment?.MaxAssignments,
                IdUserCreatedAt = c.IdUserCreatedAt,
                IdUserUpdatedAt = c.IdUserUpdatedAt,
                StatusRegister = c.StatusRegister,
                OperationRegister = c.OperationRegister
            }).ToList();
        }
    }
}
