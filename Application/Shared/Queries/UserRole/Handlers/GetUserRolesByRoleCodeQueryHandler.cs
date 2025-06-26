using Application.Shared.DTOs.TeacherResearchProfile;
using Application.Shared.DTOs.TeachingAssignment;
using Application.Shared.DTOs.TypeTeachingAssignment;
using Application.Shared.DTOs.UserRole;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Queries.UserRole.Handlers
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

                    if (isTeacherRole)
                    {
                        // 1. Asignaciones activas por cargo y su límite
                        var assignments = await _teachingAssignmentRepository.GetAllAsync(
                            ta => ta.IdTeacher == ur.IdUser && ta.RevocationDate == null && ta.StatusRegister,
                            null);
                        var grouped = assignments
                            .GroupBy(ta => ta.IdTypeTeachingAssignment)
                            .ToList();
                        var cargos = new List<TeachingAssignmentCargoInfoDto>();
                        foreach (var group in grouped)
                        {
                            var first = group.First();
                            var typeCargo = first.TypeTeachingAssignment;
                            cargos.Add(new TeachingAssignmentCargoInfoDto
                            {
                                IdTypeTeachingAssignment = group.Key,
                                CargoName = typeCargo?.Name ?? string.Empty,
                                MaxAssignments = typeCargo?.MaxAssignments,
                                ActiveAssignments = group.Select(_mapper.Map<TeachingAssignmentDto>).ToList()
                            });
                        }
                        dto.TeachingAssignmentsByCargo = cargos;

                        // 2. Perfiles de investigación
                        var profiles = await _teacherResearchProfileRepository.GetAllAsync(
                            p => p.IdUser == ur.IdUser && p.StatusRegister,
                            null);
                        dto.TeacherResearchProfiles = profiles.Select(_mapper.Map<TeacherResearchProfileDto>).ToList();
                    }
                    resultItems.Add(dto);
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