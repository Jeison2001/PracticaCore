using Api.Authorization;
using Application.Shared.DTOs;
using Application.Shared.DTOs.ResearchLines;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class ResearchLineController : GenericController<ResearchLine, int, ResearchLineDto>
    {
        public ResearchLineController(IMediator mediator) : base(mediator)
        {
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINL)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] ResearchLineDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINL)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<ResearchLineDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINL)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] ResearchLineDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFINL)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}