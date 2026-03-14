using Application.Shared.DTOs.Roles;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Queries.Roles.Handlers
{
    public class GetRolesByUserIdQueryHandler : IRequestHandler<GetRolesByUserIdQuery, List<RoleDto>>
    {
        private readonly IUserInfoRepository _userInfoRepository;
        private readonly IRepository<Role, int> _roleRepository;
        public GetRolesByUserIdQueryHandler(IUserInfoRepository userInfoRepository, IRepository<Role, int> roleRepository)
        {
            _userInfoRepository = userInfoRepository;
            _roleRepository = roleRepository;
        }
        public async Task<List<RoleDto>> Handle(GetRolesByUserIdQuery request, CancellationToken cancellationToken)
        {
            var userRoleNames = await _userInfoRepository.GetUserRolesAsync(request.UserId);
            var allRoles = await _roleRepository.GetAllAsync(r => userRoleNames.Contains(r.Name));
            return allRoles.Select(r => new RoleDto
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                IdUserCreatedAt = r.IdUserCreatedAt
            }).ToList();
        }
    }
}