using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.StateStages;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class StateStageController : GenericController<StateStage, int, StateStageDto>
    {
        public StateStageController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] StateStageDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<StateStageDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] StateStageDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}
