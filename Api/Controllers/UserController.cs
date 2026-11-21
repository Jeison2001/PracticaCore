using Api.Authorization;
using Api.Responses;
using Application.Common.Services.Jobs;
using Application.Shared.Commands;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Users;
using Application.Shared.Queries;
using Application.Shared.Queries.Users;
using Domain.Common;
using Domain.Common.Auth;
using Domain.Common.Extensions;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Services.Auth;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class UserController : GenericController<User, int, UserDto>
    {
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly IRefreshTokenService _refreshTokenService;

        public UserController(
            IMediator mediator,
            IJobEnqueuer jobEnqueuer,
            IRefreshTokenService refreshTokenService) : base(mediator)
        {
            _jobEnqueuer = jobEnqueuer;
            _refreshTokenService = refreshTokenService;
        }

        [RequirePermission(PermissionCodes.Configuraciones.N2CFP, PermissionCodes.Configuraciones.N3CFSU)]
        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            if (this.IsCrossActorAccessDenied(id, PermissionCodes.Configuraciones.N3CFSU))
                return Forbid();

            return await base.GetById(id);
        }

        /// <summary>
        /// Obtiene usuarios paginados incluyendo sus roles activos asignados (batch fetch, zero N+1).
        /// </summary>
        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpGet]
        public override async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllUsersWithRolesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters,
                CursorCreatedAt = request.CursorCreatedAt,
                CursorId = request.CursorId,
                SkipTotalCount = request.SkipTotalCount
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<UserDto>> { Success = true, Data = result });
        }

        /// <summary>
        /// Busca un estudiante activo por identificación exponiendo únicamente datos mínimos de contacto.
        /// </summary>
        [RequirePermission(PermissionCodes.ModalidadesGrado.N2MGR)]
        [HttpGet("student-lookup")]
        public async Task<IActionResult> StudentLookup([FromQuery] string? identification)
        {
            if (string.IsNullOrWhiteSpace(identification))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "El número de identificación es requerido." }
                });
            }

            var result = await _mediator.Send(
                new GetStudentLookupByIdentificationQuery(identification),
                HttpContext?.RequestAborted ?? CancellationToken.None);

            if (result == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "No se encontró ningún estudiante activo con esta identificación." }
                });
            }

            return Ok(new ApiResponse<StudentLookupDto> { Success = true, Data = result });
        }

        /// <summary>
        /// Crea un usuario y encola la notificación institucional de bienvenida (USER_CREATED).
        /// </summary>
        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] UserDto dto)
        {
            var result = await _mediator.Send(new CreateEntityCommand<User, int, UserDto>(dto, User.GetCurrentUserInfo()));

            // Encolar job de notificación en segundo plano
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleUserCreationAsync(result.Id));

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<UserDto> { Success = true, Data = result });
        }

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<UserDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [RequirePermission(PermissionCodes.Configuraciones.N2CFP, PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Forbid();

            if (this.IsCrossActorAccessDenied(id, PermissionCodes.Configuraciones.N3CFSU))
                return Forbid();

            var permissions = PermissionClaimHelper.ExtractPermissionCodes(User);
            if (!permissions.Contains(PermissionCodes.Configuraciones.N3CFSU))
            {
                var existingUser = await _mediator.Send(new GetEntityByIdQuery<User, int, UserDto>(id));
                if (!existingUser.StatusRegister)
                    return Forbid();

                // Usuario no administrador (N2CFP) solo puede actualizar campos de perfil propios (nombre/teléfono);
                // preservar campos académicos, de identificación y estado.
                dto = dto with
                {
                    Id = existingUser.Id,
                    StatusRegister = existingUser.StatusRegister,
                    Email = existingUser.Email,
                    Identification = existingUser.Identification,
                    IdIdentificationType = existingUser.IdIdentificationType,
                    IdAcademicProgram = existingUser.IdAcademicProgram,
                    IdStudyPlan = existingUser.IdStudyPlan,
                    CurrentAcademicPeriod = existingUser.CurrentAcademicPeriod,
                    CumulativeAverage = existingUser.CumulativeAverage,
                    ApprovedCredits = existingUser.ApprovedCredits,
                    TotalAcademicCredits = existingUser.TotalAcademicCredits,
                    Observation = existingUser.Observation,
                    Roles = null
                };
            }

            var result = await base.Update(id, dto);

            if (permissions.Contains(PermissionCodes.Configuraciones.N3CFSU) &&
                !dto.StatusRegister &&
                IsSuccessStatusResult(result))
            {
                await _refreshTokenService.RevokeAllTokensForUserAsync(id, HttpContext?.RequestAborted ?? CancellationToken.None);
            }

            return result;
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
        {
            var result = await base.UpdateStatus(id, dto);

            if (!dto.StatusRegister && IsSuccessStatusResult(result))
            {
                await _refreshTokenService.RevokeAllTokensForUserAsync(id, HttpContext?.RequestAborted ?? CancellationToken.None);
            }

            return result;
        }

        private static bool IsSuccessStatusResult(IActionResult result) => result switch
        {
            ObjectResult objResult => objResult.StatusCode is null or (>= 200 and < 300),
            StatusCodeResult statusResult => statusResult.StatusCode is >= 200 and < 300,
            _ => false
        };
    }
}