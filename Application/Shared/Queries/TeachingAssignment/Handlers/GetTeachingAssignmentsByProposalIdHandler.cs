using Application.Shared.DTOs.TeachingAssignment;
using Application.Shared.Queries.TeachingAssignment;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Queries.TeachingAssignment.Handlers
{
    public class GetTeachingAssignmentsByProposalIdHandler : IRequestHandler<GetTeachingAssignmentsByProposalIdQuery, List<TeachingAssignmentTeacherDto>>
    {
        private readonly IRepository<Domain.Entities.TeachingAssignment, int> _teachingAssignmentRepository;
        private readonly IRepository<User, int> _userRepository;
        private readonly IRepository<TypeTeachingAssignment, int> _typeTeachingAssignmentRepository;

        public GetTeachingAssignmentsByProposalIdHandler(
            IRepository<Domain.Entities.TeachingAssignment, int> teachingAssignmentRepository,
            IRepository<User, int> userRepository,
            IRepository<TypeTeachingAssignment, int> typeTeachingAssignmentRepository)
        {
            _teachingAssignmentRepository = teachingAssignmentRepository;
            _userRepository = userRepository;
            _typeTeachingAssignmentRepository = typeTeachingAssignmentRepository;
        }

        public async Task<List<TeachingAssignmentTeacherDto>> Handle(GetTeachingAssignmentsByProposalIdQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _teachingAssignmentRepository.GetAllAsync(
                filter: ta => ta.IdInscriptionModality == request.ProposalId
            );

            var userIds = assignments.Select(ta => ta.IdTeacher).Distinct().ToList();
            var typeIds = assignments.Select(ta => ta.IdTypeTeachingAssignment).Distinct().ToList();

            var users = (await _userRepository.GetAllAsync(u => userIds.Contains(u.Id))).ToDictionary(u => u.Id);
            var types = (await _typeTeachingAssignmentRepository.GetAllAsync(t => typeIds.Contains(t.Id))).ToDictionary(t => t.Id);

            var result = assignments.Select(ta => new TeachingAssignmentTeacherDto
            {
                Id = ta.Id, // Id del registro TeachingAssignment
                IdUser = ta.IdTeacher, // Id del docente
                FullName = users.TryGetValue(ta.IdTeacher, out var user) ? $"{user.FirstName} {user.LastName}" : string.Empty,
                Email = users.TryGetValue(ta.IdTeacher, out var user2) ? user2.Email : string.Empty,
                IdTypeTeachingAssignment = ta.IdTypeTeachingAssignment, // Id del tipo de asignación
                AssignmentType = types.TryGetValue(ta.IdTypeTeachingAssignment, out var type) ? type.Name : string.Empty
            }).ToList();

            return result;
        }
    }
}