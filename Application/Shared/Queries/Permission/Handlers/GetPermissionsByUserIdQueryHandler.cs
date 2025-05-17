using Application.Shared.DTOs.Permission;
using Domain.Interfaces.Auth;
using MediatR;

namespace Application.Shared.Queries.Permission.Handlers
{
    public class GetPermissionsByUserIdQueryHandler : IRequestHandler<GetPermissionsByUserIdQuery, List<PermissionDto>>
    {
        private readonly IUserInfoRepository _userInfoRepository;
        public GetPermissionsByUserIdQueryHandler(IUserInfoRepository userInfoRepository)
        {
            _userInfoRepository = userInfoRepository;
        }
        public async Task<List<PermissionDto>> Handle(GetPermissionsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var permissions = await _userInfoRepository.GetUserPermissionsFullInfoAsync(request.UserId);
            return permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                ParentCode = p.ParentCode,
                Description = p.Description,
                IdUserCreatedAt = p.IdUserCreatedAt
            }).ToList();
        }
    }
}
