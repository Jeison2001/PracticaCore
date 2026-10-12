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
            var usingKeyset = (request.CursorCreatedAt.HasValue || request.CursorId.HasValue) && request.PageSize > 0;
            Func<IQueryable<User>, IOrderedQueryable<User>>? orderBy = null;

            if (usingKeyset)
            {
                // Keyset: orden forzado a (CreatedAt DESC, Id DESC) para estabilidad del cursor
                orderBy = query => query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id);
            }
            else if (!string.IsNullOrEmpty(request.SortBy))
            {
                orderBy = query =>
                {
                    var ordered = request.IsDescending
                        ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                        : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
                    // Desempate estable por Id
                    return ordered.ThenByDescending(e => e.Id);
                };
            }
            else
            {
                orderBy = query => query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id);
            }

            Expression<Func<User, bool>>? filter = FilterBuilder.BuildFilter<User, int>(request.Filters);

            var paginatedResult = await _userRepository.GetAllWithPaginationAsync(
                filter: filter,
                orderBy: orderBy,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: ct,
                cursorId: request.CursorId,
                cursorCreatedAt: request.CursorCreatedAt,
                skipTotalCount: request.SkipTotalCount == true
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
                PageSize = paginatedResult.PageSize,
                HasMoreRows = paginatedResult.HasMoreRows,
                CursorCreatedAt = paginatedResult.CursorCreatedAt,
                CursorId = paginatedResult.CursorId
            };
        }
    }
}
