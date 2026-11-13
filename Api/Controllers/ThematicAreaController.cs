using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.ThematicAreas;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class ThematicAreaController : GenericController<ThematicArea, int, ThematicAreaDto>
    {
        public ThematicAreaController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINA)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] ThematicAreaDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINA)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<ThematicAreaDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINA)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] ThematicAreaDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINA)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}