using Application.Shared.DTOs.TeacherEnabledCargos;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.TeacherEnabledCargos.Handlers
{
    public class GetTeachersByCargoQueryHandler : IRequestHandler<GetTeachersByCargoQuery, List<TeacherByCargoDto>>
    {
        private readonly ITeacherEnabledCargoRepository _repository;

        public GetTeachersByCargoQueryHandler(ITeacherEnabledCargoRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TeacherByCargoDto>> Handle(GetTeachersByCargoQuery request, CancellationToken cancellationToken)
        {
            var teacherCargos = await _repository.GetByCargoIdAsync(request.CargoId, cancellationToken);

            return teacherCargos.Select(tc => new TeacherByCargoDto
            {
                IdUser = tc.IdUser,
                FullName = $"{tc.User.FirstName} {tc.User.LastName}".Trim(),
                Email = tc.User.Email,
                Identification = tc.User.Identification,
                IdTypeTeachingAssignment = tc.IdTypeTeachingAssignment
            }).ToList();
        }
    }
}
