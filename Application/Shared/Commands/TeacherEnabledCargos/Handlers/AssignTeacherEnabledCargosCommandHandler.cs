using Application.Shared.DTOs.TeacherEnabledCargos;
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
            await _repository.BulkAssignAsync(
                request.UserId,
                request.TypeTeachingAssignmentIds,
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
