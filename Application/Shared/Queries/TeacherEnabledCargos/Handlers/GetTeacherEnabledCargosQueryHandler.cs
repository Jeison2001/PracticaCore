using Application.Shared.DTOs.TeacherEnabledCargos;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.TeacherEnabledCargos.Handlers
{
    public class GetTeacherEnabledCargosQueryHandler : IRequestHandler<GetTeacherEnabledCargosQuery, List<TeacherEnabledCargoDto>>
    {
        private readonly ITeacherEnabledCargoRepository _repository;

        public GetTeacherEnabledCargosQueryHandler(ITeacherEnabledCargoRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TeacherEnabledCargoDto>> Handle(GetTeacherEnabledCargosQuery request, CancellationToken cancellationToken)
        {
            var cargos = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

            return cargos.Select(c => new TeacherEnabledCargoDto
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
