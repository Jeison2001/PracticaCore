using Application.Shared.DTOs.Role;
using Domain.Interfaces.Auth;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.Role.Handlers
{
    public class GetRolesByUserIdQueryHandler : IRequestHandler<GetRolesByUserIdQuery, List<RoleDto>>
    {
        private readonly IUserInfoRepository _userInfoRepository;
        private readonly IRepository<Domain.Entities.Role, int> _roleRepository;
        public GetRolesByUserIdQueryHandler(IUserInfoRepository userInfoRepository, IRepository<Domain.Entities.Role, int> roleRepository)
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
