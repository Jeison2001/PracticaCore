using Api.Authorization;
using Api.Responses;
using Application.Common.Services.Jobs;
using Application.Shared.Commands.Proposals;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Proposals;
using Application.Shared.Queries.Proposals;
using Domain.Common;
using Domain.Common.Auth;
using Domain.Common.Extensions;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Propuestas de Grado. Override de Create y GetAll del GenericController
    /// (usa CreateProposalCommand con validaciones específicas y queries con details).
    /// Patch cambia estado y encola notificaciones.
    /// </summary>
    public class ProposalController : GenericController<Proposal, int, ProposalDto>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer? _jobEnqueuer;

        public ProposalController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            IJobEnqueuer? jobEnqueuer = null) : base(mediator)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
        }

        [RequirePermission(PermissionCodes.ProyectoGrado.N4PGPPR)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] ProposalPatchDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchProposalCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<ProposalDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.ProyectoGrado.N3PGRP)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProposalDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new CreateProposalCommand(dto, currentUser);
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<ProposalDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.ProyectoGrado.N4PGPPA, PermissionCodes.ProyectoGrado.N4PGPPG)]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllProposalsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters,
                CursorId = request.CursorId,
                CursorCreatedAt = request.CursorCreatedAt,
                SkipTotalCount = request.SkipTotalCount
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [RequirePermission(
            PermissionCodes.ProyectoGrado.N3PGCP,
            PermissionCodes.ProyectoGrado.N4PGPPA,
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPR,
            PermissionCodes.ProyectoGrado.N4PGPPC)]
        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var query = new GetProposalWithDetailsQuery(id);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<ProposalWithDetailsResponseDto> { Success = true, Data = result });
        }

        [RequirePermission(
            PermissionCodes.ProyectoGrado.N3PGRP,
            PermissionCodes.ProyectoGrado.N3PGCP,
            PermissionCodes.ProyectoGrado.N3PGRA,
            PermissionCodes.ProyectoGrado.N3PGRPR)]
        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetProposalsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<ProposalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.ProyectoGrado.N4PGPPR, PermissionCodes.ProyectoGrado.N4PGPPC)]
        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id,
            [FromQuery] PaginatedRequest request)
        {
            var query = new GetProposalsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? "",
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>())
            {
                CursorId = request.CursorId,
                CursorCreatedAt = request.CursorCreatedAt,
                SkipTotalCount = request.SkipTotalCount
            };

            var result = await _mediator.Send(query);

            return Ok(new ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>> {
                Success = true,
                Data = result,
                Messages = new List<string> { $"Se encontraron {result.TotalRecords} propuestas asignadas al docente." }
            });
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Create([FromBody] ProposalDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [RequirePermission(PermissionCodes.ProyectoGrado.N4PGPPR, PermissionCodes.ProyectoGrado.N3PGRP, PermissionCodes.ProyectoGrado.N3PGCP)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] ProposalDto dto)
        {
            int? previousStateId = null;

            if (User?.Identity?.IsAuthenticated == true)
            {
                var permissions = PermissionClaimHelper.ExtractPermissionCodes(User);
                var isCommitteeOrEvaluator =
                    permissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPR) ||
                    permissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPG);

                if (!isCommitteeOrEvaluator)
                {
                    var currentUserId = User.GetUserId();
                    if (!currentUserId.HasValue)
                        return Forbid();

                    var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
                    var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                    var isMember = await userInscriptionRepo.GetFirstOrDefaultAsync(
                        x => x.IdInscriptionModality == id && x.IdUser == currentUserId.Value && x.StatusRegister,
                        cancellationToken);
                    if (isMember == null)
                        return Forbid();

                    var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                    var existingProposal = await proposalRepo.GetByIdAsync(id);
                    if (existingProposal == null)
                    {
                        return NotFound(new ApiResponse<ProposalDto>
                        {
                            Success = false,
                            Errors = new List<string> { "Propuesta no encontrada." }
                        });
                    }

                    var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                    var currentState = await stateStageRepo.GetByIdAsync(existingProposal.IdStateStage);
                    if (currentState == null
                        || !currentState.StatusRegister
                        || currentState.Code != StateStageCodes.PropNoPertinente
                        || currentState.IsFinalStateForStage
                        || currentState.IsFinalStateForModalityOverall)
                    {
                        return BadRequest(new ApiResponse<ProposalDto>
                        {
                            Success = false,
                            Errors = new List<string> { "Solo se puede subsanar una propuesta cuando se encuentra en estado Propuesta No Pertinente." }
                        });
                    }

                    var propRadicadaState = await stateStageRepo.GetFirstOrDefaultAsync(
                        s => s.Code == StateStageCodes.PropRadicada && s.StatusRegister,
                        cancellationToken);
                    if (propRadicadaState == null)
                        throw new InvalidOperationException($"Active StateStage '{StateStageCodes.PropRadicada}' was not found.");

                    previousStateId = existingProposal.IdStateStage;

                    dto = dto with
                    {
                        Id = dto.Id == 0 ? id : dto.Id,
                        IdStateStage = propRadicadaState.Id,
                        Observation = existingProposal.Observation
                    };
                }
            }

            var updateResult = await base.Update(id, dto);

            if (previousStateId.HasValue && updateResult is OkObjectResult && _jobEnqueuer != null)
            {
                var capturedPreviousStateId = previousStateId.Value;
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleProposalChangeAsync(id, capturedPreviousStateId));
            }

            return updateResult;
        }

        [RequirePermission(PermissionCodes.ProyectoGrado.N4PGPPG)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<ProposalDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}
