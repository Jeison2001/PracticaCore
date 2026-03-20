using Application.Shared.DTOs.TeacherResearchProfiles;
using Application.Shared.DTOs.TeachingAssignments;
using Application.Shared.DTOs.UserRoles;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Queries.UserRoles.Handlers
{
    public class GetUserRolesByRoleCodeQueryHandler : IRequestHandler<GetUserRolesByRoleCodeQuery, PaginatedResult<UserRoleWithUserDetailsDto>>
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
        private readonly IRepository<TeacherResearchProfile, int> _teacherResearchProfileRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserRolesByRoleCodeQueryHandler> _logger;

        public GetUserRolesByRoleCodeQueryHandler(
            IUserRoleRepository userRoleRepository,
            ITeachingAssignmentRepository teachingAssignmentRepository,
            IRepository<TeacherResearchProfile, int> teacherResearchProfileRepository,
            IMapper mapper,
            ILogger<GetUserRolesByRoleCodeQueryHandler> logger)
        {
            _userRoleRepository = userRoleRepository;
            _teachingAssignmentRepository = teachingAssignmentRepository;
            _teacherResearchProfileRepository = teacherResearchProfileRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PaginatedResult<UserRoleWithUserDetailsDto>> Handle(GetUserRolesByRoleCodeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Obtener UserRoles con detalles de Usuario y Rol usando el repositorio específico
                var userRolesResult = await _userRoleRepository.GetUserRolesWithUserDetailsAsync(
                    request.RoleCode,
                    request.PageNumber,
                    request.PageSize,
                    request.SortBy,
                    request.IsDescending,
                    request.Filters,
                    cancellationToken);
                
                // Si no hay resultados, devolver un resultado vacío
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

                // Proyectar los resultados a DTOs
                var resultItems = new List<UserRoleWithUserDetailsDto>();
                bool isTeacherRole = (request.RoleCode != null && request.RoleCode.ToUpper() == "TEACHER");
                
                if (isTeacherRole)
                {
                    // ─── Batch fetch: 1 query por tabla, NO 1 query por docente ─────────────
                    var teacherUserIds = userRolesResult.Items.Select(ur => ur.IdUser).ToList();

                    var allAssignments = (await _teachingAssignmentRepository.GetAllAsync(
                        ta => teacherUserIds.Contains(ta.IdTeacher) && ta.RevocationDate == null && ta.StatusRegister,
                        null)).ToList();

                    var allProfiles = (await _teacherResearchProfileRepository.GetAllAsync(
                        p => teacherUserIds.Contains(p.IdUser) && p.StatusRegister,
                        null)).ToList();

                    var assignmentsByTeacher = allAssignments
                        .GroupBy(ta => ta.IdTeacher)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    var profilesByTeacher = allProfiles
                        .GroupBy(p => p.IdUser)
                        .ToDictionary(g => g.Key, g => g.ToList());
                    // ────────────────────────────────────────────────────────────────────────

                    foreach (var ur in userRolesResult.Items)
                    {
                        var dto = new UserRoleWithUserDetailsDto
                        {
                            Id = ur.Id,
                            IdUser = ur.IdUser,
                            IdRole = ur.IdRole,
                            RoleCode = ur.Role?.Code ?? string.Empty,
                            RoleName = ur.Role?.Name ?? string.Empty,
                            Email = ur.User?.Email ?? string.Empty,
                            FirstName = ur.User?.FirstName ?? string.Empty,
                            LastName = ur.User?.LastName ?? string.Empty,
                            Identification = ur.User?.Identification ?? string.Empty,
                            IdUserCreatedAt = ur.IdUserCreatedAt ?? 0,
                            IdUserUpdatedAt = ur.IdUserUpdatedAt,
                            UpdatedAt = ur.UpdatedAt,
                            StatusRegister = ur.StatusRegister,
                            OperationRegister = ur.OperationRegister
                        };

                        var teacherAssignments = assignmentsByTeacher.GetValueOrDefault(ur.IdUser, new());
                        var grouped = teacherAssignments.GroupBy(ta => ta.IdTypeTeachingAssignment).ToList();
                        var cargos = grouped.Select(group =>
                        {
                            var typeCargo = group.First().TypeTeachingAssignment;
                            return new TeachingAssignmentCargoInfoDto
                            {
                                IdTypeTeachingAssignment = group.Key,
                                CargoName = typeCargo?.Name ?? string.Empty,
                                MaxAssignments = typeCargo?.MaxAssignments,
                                ActiveAssignments = group.Select(_mapper.Map<TeachingAssignmentDto>).ToList()
                            };
                        }).ToList();
                        dto.TeachingAssignmentsByCargo = cargos;

                        dto.TeacherResearchProfiles = profilesByTeacher
                            .GetValueOrDefault(ur.IdUser, new())
                            .Select(_mapper.Map<TeacherResearchProfileDto>)
                            .ToList();

                        resultItems.Add(dto);
                    }
                }
                else
                {
                    foreach (var ur in userRolesResult.Items)
                    {
                        resultItems.Add(new UserRoleWithUserDetailsDto
                        {
                            Id = ur.Id,
                            IdUser = ur.IdUser,
                            IdRole = ur.IdRole,
                            RoleCode = ur.Role?.Code ?? string.Empty,
                            RoleName = ur.Role?.Name ?? string.Empty,
                            Email = ur.User?.Email ?? string.Empty,
                            FirstName = ur.User?.FirstName ?? string.Empty,
                            LastName = ur.User?.LastName ?? string.Empty,
                            Identification = ur.User?.Identification ?? string.Empty,
                            IdUserCreatedAt = ur.IdUserCreatedAt ?? 0,
                            IdUserUpdatedAt = ur.IdUserUpdatedAt,
                            UpdatedAt = ur.UpdatedAt,
                            StatusRegister = ur.StatusRegister,
                            OperationRegister = ur.OperationRegister
                        });
                    }
                }

                // Retornar el resultado paginado
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