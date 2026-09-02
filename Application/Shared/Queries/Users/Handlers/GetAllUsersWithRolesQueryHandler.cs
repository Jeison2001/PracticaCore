using Application.Shared.DTOs;
using Application.Shared.DTOs.Roles;
using Application.Shared.DTOs.Users;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Shared.Queries.Users.Handlers
{
    public class GetAllUsersWithRolesQueryHandler : IRequestHandler<GetAllUsersWithRolesQuery, PaginatedResult<UserDto>>
    {
        private readonly IRepository<User, int> _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IMapper _mapper;

        public GetAllUsersWithRolesQueryHandler(
            IRepository<User, int> userRepository,
            IUserRoleRepository userRoleRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<UserDto>> Handle(GetAllUsersWithRolesQuery request, CancellationToken ct)
        {
            Func<IQueryable<User>, IOrderedQueryable<User>>? orderBy = null;
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                orderBy = query => request.IsDescending
                    ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                    : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
            }

            Expression<Func<User, bool>>? filter = FilterBuilder.BuildFilter<User, int>(request.Filters);

            var paginatedResult = await _userRepository.GetAllWithPaginationAsync(
                filter: filter,
                orderBy: orderBy,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize
            );

            var userDtos = _mapper.Map<List<UserDto>>(paginatedResult.Items);
            var userIds = userDtos.Select(u => u.Id).ToList();

            if (userIds.Any())
            {
                var userRoles = await _userRoleRepository.GetRolesByUserIdsAsync(userIds, ct);
                var rolesByUserId = userRoles
                    .GroupBy(ur => ur.IdUser)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(ur => new RoleSimpleDto
                        {
                            Id = ur.Role.Id,
                            Code = ur.Role.Code,
                            Name = ur.Role.Name
                        }).ToList()
                    );

                foreach (var dto in userDtos)
                {
                    dto.Roles = rolesByUserId.GetValueOrDefault(dto.Id, new List<RoleSimpleDto>());
                }
            }

            return new PaginatedResult<UserDto>
            {
                Items = userDtos,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize
            };
        }
    }
}
