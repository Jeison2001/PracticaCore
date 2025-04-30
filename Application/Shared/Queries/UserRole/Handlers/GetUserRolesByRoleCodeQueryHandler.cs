using Application.Shared.DTOs.UserRole;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Queries.UserRole.Handlers
{
    public class GetUserRolesByRoleCodeQueryHandler : IRequestHandler<GetUserRolesByRoleCodeQuery, PaginatedResult<UserRoleWithUserDetailsDto>>
    {
        private readonly IRepository<Domain.Entities.UserRole, int> _userRoleRepository;
        private readonly IRepository<Role, int> _roleRepository;
        private readonly IRepository<User, int> _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserRolesByRoleCodeQueryHandler> _logger;

        public GetUserRolesByRoleCodeQueryHandler(
            IRepository<Domain.Entities.UserRole, int> userRoleRepository,
            IRepository<Role, int> roleRepository,
            IRepository<User, int> userRepository,
            IMapper mapper,
            ILogger<GetUserRolesByRoleCodeQueryHandler> logger)
        {
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PaginatedResult<UserRoleWithUserDetailsDto>> Handle(GetUserRolesByRoleCodeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Si se proporciona un código de rol, buscar el rol por su código
                Role? role = null;
                if (!string.IsNullOrEmpty(request.RoleCode))
                {
                    // Corregido: Usar ToLower() en ambos lados de la comparación en lugar de Equals() con StringComparison
                    // que no es soportado por EF Core en la traducción a SQL
                    string roleCodeLower = request.RoleCode.ToLower();
                    role = await _roleRepository.GetFirstOrDefaultAsync(
                        r => r.Code.ToLower() == roleCodeLower && r.StatusRegister,
                        cancellationToken);
                    
                    if (role == null)
                    {
                        return new PaginatedResult<UserRoleWithUserDetailsDto>
                        {
                            Items = new List<UserRoleWithUserDetailsDto>(),
                            TotalRecords = 0,
                            PageNumber = request.PageNumber,
                            PageSize = request.PageSize
                        };
                    }
                }

                // 2. Crear la expresión de filtro para UserRole
                Expression<Func<Domain.Entities.UserRole, bool>> userRoleFilter;
                
                if (role != null)
                {
                    // Si tenemos un rol específico, filtrar por su ID
                    userRoleFilter = ur => ur.IdRole == role.Id && ur.StatusRegister;
                }
                else
                {
                    // Sin filtro de rol, aplicar solo los filtros generales
                    userRoleFilter = ur => ur.StatusRegister;
                }

                // Aplicar filtros adicionales si existen
                if (request.Filters != null && request.Filters.Any())
                {
                    // Combinar con los filtros proporcionados en la solicitud
                    var additionalFilter = FilterBuilder.BuildFilter<Domain.Entities.UserRole, int>(request.Filters);
                    if (additionalFilter != null)
                    {
                        // Combinar ambos filtros (no es posible combinar directamente con &&)
                        var filter1 = userRoleFilter.Compile();
                        var filter2 = additionalFilter.Compile();
                        userRoleFilter = ur => filter1(ur) && filter2(ur);
                    }
                }

                // 3. Configurar la ordenación
                Func<IQueryable<Domain.Entities.UserRole>, IOrderedQueryable<Domain.Entities.UserRole>>? orderBy = null;
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    orderBy = query => request.IsDescending
                        ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                        : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
                }

                // 4. Obtener los UserRoles con paginación
                var userRolesResult = await _userRoleRepository.GetAllWithPaginationAsync(
                    filter: userRoleFilter,
                    orderBy: orderBy,
                    pageNumber: request.PageNumber,
                    pageSize: request.PageSize
                );

                // Verificar si la lista está vacía
                if (userRolesResult.Items == null || !userRolesResult.Items.Any())
                {
                    return new PaginatedResult<UserRoleWithUserDetailsDto>
                    {
                        Items = new List<UserRoleWithUserDetailsDto>(),
                        TotalRecords = 0,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize
                    };
                }

                // 5. Obtener los IDs de usuarios y roles
                var userIds = userRolesResult.Items.Select(ur => ur.IdUser).Distinct().ToList();
                var roleIds = userRolesResult.Items.Select(ur => ur.IdRole).Distinct().ToList();

                // 6. Obtener información de usuarios y roles
                var users = await _userRepository.GetAllAsync(
                    filter: u => userIds.Contains(u.Id) && u.StatusRegister);
                
                var roles = await _roleRepository.GetAllAsync(
                    filter: r => roleIds.Contains(r.Id) && r.StatusRegister);

                // Crear diccionarios para rápido acceso
                var userDictionary = users.ToDictionary(u => u.Id);
                var roleDictionary = roles.ToDictionary(r => r.Id);

                // 7. Crear la lista de resultados
                var resultItems = new List<UserRoleWithUserDetailsDto>();
                
                foreach (var userRole in userRolesResult.Items)
                {
                    var dto = new UserRoleWithUserDetailsDto
                    {
                        Id = userRole.Id,
                        IdUser = userRole.IdUser,
                        IdRole = userRole.IdRole,
                        IdUserCreatedAt = userRole.IdUserCreatedAt ?? 0,
                        // No asignamos CreatedAt directamente ya que tiene un setter privado
                        IdUserUpdatedAt = userRole.IdUserUpdatedAt,
                        UpdatedAt = userRole.UpdatedAt,
                        StatusRegister = userRole.StatusRegister,
                        OperationRegister = userRole.OperationRegister
                    };

                    // Añadir datos del rol
                    if (roleDictionary.TryGetValue(userRole.IdRole, out var role2))
                    {
                        dto.RoleCode = role2.Code;
                        dto.RoleName = role2.Name;
                    }

                    // Añadir datos básicos del usuario
                    if (userDictionary.TryGetValue(userRole.IdUser, out var user))
                    {
                        dto.Email = user.Email;
                        dto.FirstName = user.FirstName;
                        dto.LastName = user.LastName;
                        dto.Identification = user.Identification;
                    }

                    resultItems.Add(dto);
                }

                // 8. Retornar el resultado paginado
                return new PaginatedResult<UserRoleWithUserDetailsDto>
                {
                    Items = resultItems,
                    TotalRecords = userRolesResult.TotalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios por rol: {Message}", ex.Message);
                throw;
            }
        }
    }
}